using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Users;

/// <summary>
/// T073 — US-07: o perfil público devolve username/avatar/contagens e NÃO expõe PII (email).
/// </summary>
public class ProfileTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private const string Username = "profile_tester_001";
    private readonly CatalogApiFactory _factory = factory;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    private async Task<Guid> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Username == Username);
        if (existing is not null) return existing.Id;
        var user = new User { Id = Guid.NewGuid(), Username = Username, Email = "secret@example.com", Role = "user" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task CleanupAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.RemoveRange(db.Users.Where(u => u.Id == userId));
        await db.SaveChangesAsync();
    }

    private record Profile(string Username, string? AvatarUrl, int FavoritesCount, int ReviewsCount);

    [Fact]
    public async Task PublicProfile_returns_counts_without_pii()
    {
        var userId = await SeedAsync();
        try
        {
            var client = _factory.CreateClient();
            var res = await client.GetAsync($"/v1/users/{Username}");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var raw = await res.Content.ReadAsStringAsync();
            Assert.DoesNotContain("secret@example.com", raw); // sem PII
            Assert.DoesNotContain("email", raw);

            var profile = await res.Content.ReadFromJsonAsync<Profile>(Json);
            Assert.Equal(Username, profile!.Username);
            Assert.Equal(0, profile.FavoritesCount);
        }
        finally
        {
            await CleanupAsync(userId);
        }
    }

    [Fact]
    public async Task PublicProfile_returns_404_for_unknown_user()
    {
        var res = await _factory.CreateClient().GetAsync("/v1/users/nao-existe-zzz");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
