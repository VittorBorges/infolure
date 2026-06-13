using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Favorites;

/// <summary>
/// T053 — US-05: adicionar, listar e remover favoritos pelos endpoints autenticados.
/// Usa o esquema de auth de teste (sub = TestAuthHandler.Sub) e um utilizador semeado.
/// </summary>
public class FavoritesTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };
    private readonly AuthenticatedApiFactory _factory = factory;

    private async Task<(Guid userId, Guid lureId)> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // utilizador de teste ligado ao sub do esquema de auth de teste
        var userId = await db.UserAuthProviders
            .Where(p => p.ProviderUid == TestAuthHandler.Sub).Select(p => (Guid?)p.UserId).FirstOrDefaultAsync()
            ?? Guid.Empty;
        if (userId == Guid.Empty)
        {
            var user = new User { Id = Guid.NewGuid(), Username = "fav_tester", Role = "user" };
            db.Users.Add(user);
            db.UserAuthProviders.Add(new UserAuthProvider
            {
                Id = Guid.NewGuid(), UserId = user.Id, Provider = "test", ProviderUid = TestAuthHandler.Sub,
            });
            await db.SaveChangesAsync();
            userId = user.Id;
        }

        var lureId = await db.Lures.Where(l => l.Slug == "isca-001").Select(l => l.Id).FirstAsync();
        // limpa favorito remanescente de execuções anteriores
        var stale = await db.UserLureFavorites.Where(f => f.UserId == userId && f.LureId == lureId).ToListAsync();
        db.UserLureFavorites.RemoveRange(stale);
        await db.SaveChangesAsync();
        return (userId, lureId);
    }

    private async Task CleanupAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.UserLureFavorites.RemoveRange(db.UserLureFavorites.Where(f => f.UserId == userId));
        db.UserAuthProviders.RemoveRange(db.UserAuthProviders.Where(p => p.UserId == userId));
        db.Users.RemoveRange(db.Users.Where(u => u.Id == userId));
        await db.SaveChangesAsync();
    }

    private record ListResp(List<Card> Data, Meta Meta);
    private record Card(Guid Id, string Slug, bool? IsFavorited);
    private record Meta(int Total);

    [Fact]
    public async Task Add_list_remove_favorite_roundtrip()
    {
        var (userId, lureId) = await SeedAsync();
        try
        {
            var client = _factory.CreateClient();

            var add = await client.PostAsync($"/v1/me/favorites/{lureId}", null);
            Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

            var afterAdd = await client.GetFromJsonAsync<ListResp>("/v1/me/favorites", Json);
            Assert.NotNull(afterAdd);
            Assert.Contains(afterAdd!.Data, c => c.Id == lureId && c.IsFavorited == true);

            var remove = await client.DeleteAsync($"/v1/me/favorites/{lureId}");
            Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);

            var afterRemove = await client.GetFromJsonAsync<ListResp>("/v1/me/favorites", Json);
            Assert.DoesNotContain(afterRemove!.Data, c => c.Id == lureId);
        }
        finally
        {
            await CleanupAsync(userId);
        }
    }
}
