using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Admin;

/// <summary>
/// Backoffice (T078-T081): autorização admin, recálculo de preços e moderação de reviews.
/// </summary>
public class AdminTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };
    private readonly AuthenticatedApiFactory _factory = factory;

    private HttpClient Admin()
    {
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Add("X-Test-Role", "admin");
        return c;
    }

    private record Price(decimal? Price6mMinEur, decimal? Price6mMaxEur, decimal? Price6mAvgEur);

    [Fact]
    public async Task NonAdmin_is_forbidden()
    {
        var client = _factory.CreateClient(); // autenticado mas sem role=admin
        var res = await client.PostAsJsonAsync("/v1/admin/brands", new { slug = "x", name = "X" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AddPrice_recomputes_avg_min_max()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var lureId0 = await db.Lures.Where(l => l.Slug == "isca-004").Select(l => l.Id).FirstAsync();
            db.LureRetailerPrices.RemoveRange(db.LureRetailerPrices.Where(p => p.LureId == lureId0));
            await db.SaveChangesAsync();
        }

        var admin = Admin();
        Guid lureId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            lureId = await db.Lures.Where(l => l.Slug == "isca-004").Select(l => l.Id).FirstAsync();
        }

        var p1 = await (await admin.PostAsJsonAsync($"/v1/admin/lures/{lureId}/prices",
            new { retailer = "LojaA", price_eur = 10.00 })).Content.ReadFromJsonAsync<Price>(Json);
        Assert.Equal(10.00m, p1!.Price6mAvgEur);

        var p2 = await (await admin.PostAsJsonAsync($"/v1/admin/lures/{lureId}/prices",
            new { retailer = "LojaB", price_eur = 20.00 })).Content.ReadFromJsonAsync<Price>(Json);
        Assert.Equal(10.00m, p2!.Price6mMinEur);
        Assert.Equal(20.00m, p2.Price6mMaxEur);
        Assert.Equal(15.00m, p2.Price6mAvgEur);

        // cleanup
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.LureRetailerPrices.RemoveRange(db.LureRetailerPrices.Where(p => p.LureId == lureId));
            var lure = await db.Lures.FirstAsync(l => l.Id == lureId);
            lure.Price6mMinEur = lure.Price6mMaxEur = lure.Price6mAvgEur = null;
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Moderate_hides_review_from_public_list()
    {
        Guid userId, reviewId;
        const string slug = "isca-005";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var lureId = await db.Lures.Where(l => l.Slug == slug).Select(l => l.Id).FirstAsync();
            var user = new User { Id = Guid.NewGuid(), Username = "mod_target", Role = "user" };
            db.Users.Add(user);
            var review = new LureReview { Id = Guid.NewGuid(), LureId = lureId, UserId = user.Id, Rating = 3, Status = "published" };
            db.LureReviews.Add(review);
            await db.SaveChangesAsync();
            userId = user.Id; reviewId = review.Id;
        }

        try
        {
            var admin = Admin();
            var mod = await admin.PatchAsJsonAsync($"/v1/admin/reviews/{reviewId}/moderation", new { status = "hidden" });
            Assert.Equal(HttpStatusCode.NoContent, mod.StatusCode);

            var listRaw = await _factory.CreateClient().GetStringAsync($"/v1/lures/{slug}/reviews");
            Assert.DoesNotContain(reviewId.ToString(), listRaw); // oculta não aparece no público
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.LureReviews.RemoveRange(db.LureReviews.Where(r => r.Id == reviewId));
            db.Users.RemoveRange(db.Users.Where(u => u.Id == userId));
            await db.SaveChangesAsync();
        }
    }
}
