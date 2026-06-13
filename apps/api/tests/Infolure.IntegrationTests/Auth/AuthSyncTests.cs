using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Infolure.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Auth;

/// <summary>
/// T044 — US-04: o webhook /v1/auth/sync cria o utilizador (com username pendente)
/// e é idempotente em (provider, provider_uid). Verificável contra o Postgres local.
/// </summary>
public class AuthSyncTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };
    private readonly CatalogApiFactory _factory = factory;
    private const string TestUid = "test-sub-uid-0001";

    private async Task CleanupAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var links = await db.UserAuthProviders.Where(p => p.ProviderUid == TestUid).ToListAsync();
        var userIds = links.Select(l => l.UserId).Distinct().ToList();
        db.UserAuthProviders.RemoveRange(links);
        db.Users.RemoveRange(db.Users.Where(u => userIds.Contains(u.Id)));
        await db.SaveChangesAsync();
    }

    private record SyncResult(Guid UserId, string? Username, bool NeedsUsername);

    [Fact]
    public async Task Sync_creates_user_then_is_idempotent()
    {
        await CleanupAsync();
        try
        {
            var client = _factory.CreateClient();
            var payload = new
            {
                provider = "google",
                provider_uid = TestUid,
                email = "tester@example.com",
                display_name = "Tester",
            };

            var first = await client.PostAsJsonAsync("/v1/auth/sync", payload);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            var firstBody = await first.Content.ReadFromJsonAsync<SyncResult>(Json);
            Assert.NotNull(firstBody);
            Assert.True(firstBody!.NeedsUsername);          // username ainda por escolher
            Assert.Null(firstBody.Username);
            Assert.NotEqual(Guid.Empty, firstBody.UserId);

            var second = await client.PostAsJsonAsync("/v1/auth/sync", payload);
            var secondBody = await second.Content.ReadFromJsonAsync<SyncResult>(Json);
            Assert.Equal(firstBody.UserId, secondBody!.UserId); // idempotente — mesmo utilizador

            // Confirma uma única linha de provider na base
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var count = await db.UserAuthProviders.CountAsync(p => p.ProviderUid == TestUid);
            Assert.Equal(1, count);
        }
        finally
        {
            await CleanupAsync();
        }
    }
}
