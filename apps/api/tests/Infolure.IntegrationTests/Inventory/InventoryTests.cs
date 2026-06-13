using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Inventory;

/// <summary>
/// T060 — US-06: CRUD de inventário pelos endpoints autenticados. Usa um `sub` próprio
/// (header X-Test-Sub) para não colidir com outras classes de teste paralelas.
/// </summary>
public class InventoryTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private const string Sub = "test-inv-sub-0001";
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };
    private readonly AuthenticatedApiFactory _factory = factory;

    private HttpClient Client()
    {
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, Sub);
        return c;
    }

    private async Task<(Guid userId, Guid lureId)> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = await db.UserAuthProviders.Where(p => p.ProviderUid == Sub)
            .Select(p => (Guid?)p.UserId).FirstOrDefaultAsync() ?? Guid.Empty;
        if (userId == Guid.Empty)
        {
            var user = new User { Id = Guid.NewGuid(), Username = "inv_tester", Role = "user" };
            db.Users.Add(user);
            db.UserAuthProviders.Add(new UserAuthProvider
            { Id = Guid.NewGuid(), UserId = user.Id, Provider = "test", ProviderUid = Sub });
            await db.SaveChangesAsync();
            userId = user.Id;
        }
        var lureId = await db.Lures.Where(l => l.Slug == "isca-002").Select(l => l.Id).FirstAsync();
        db.UserLureInventory.RemoveRange(db.UserLureInventory.Where(i => i.UserId == userId));
        await db.SaveChangesAsync();
        return (userId, lureId);
    }

    private async Task CleanupAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.UserLureInventory.RemoveRange(db.UserLureInventory.Where(i => i.UserId == userId));
        db.UserAuthProviders.RemoveRange(db.UserAuthProviders.Where(p => p.UserId == userId));
        db.Users.RemoveRange(db.Users.Where(u => u.Id == userId));
        await db.SaveChangesAsync();
    }

    private record EntryDto(Guid Id, int Quantity, string? Condition, string? Notes);
    private record ListResp(List<EntryDto> Data, int TotalUniqueLures);

    [Fact]
    public async Task Add_update_delete_inventory_roundtrip()
    {
        var (userId, lureId) = await SeedAsync();
        try
        {
            var client = Client();

            // add
            var add = await client.PostAsJsonAsync("/v1/me/inventory",
                new { lure_id = lureId, quantity = 2, condition = "good", notes = "caixa A" });
            Assert.Equal(HttpStatusCode.Created, add.StatusCode);
            var created = await add.Content.ReadFromJsonAsync<EntryDto>(Json);
            Assert.NotNull(created);
            Assert.Equal(2, created!.Quantity);

            // duplicado → 409
            var dup = await client.PostAsJsonAsync("/v1/me/inventory", new { lure_id = lureId, quantity = 1 });
            Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

            // list
            var list = await client.GetFromJsonAsync<ListResp>("/v1/me/inventory", Json);
            Assert.Equal(1, list!.TotalUniqueLures);

            // update
            var patch = await client.PatchAsJsonAsync($"/v1/me/inventory/{created.Id}",
                new { quantity = 5, condition = "lost" });
            Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
            var updated = await patch.Content.ReadFromJsonAsync<EntryDto>(Json);
            Assert.Equal(5, updated!.Quantity);
            Assert.Equal("lost", updated.Condition);

            // validação: quantidade inválida → 422
            var bad = await client.PatchAsJsonAsync($"/v1/me/inventory/{created.Id}", new { quantity = 999 });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);

            // delete
            var del = await client.DeleteAsync($"/v1/me/inventory/{created.Id}");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

            var after = await client.GetFromJsonAsync<ListResp>("/v1/me/inventory", Json);
            Assert.Equal(0, after!.TotalUniqueLures);
        }
        finally
        {
            await CleanupAsync(userId);
        }
    }
}
