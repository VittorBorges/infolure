using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Reviews;

/// <summary>
/// T066/T067 — US-08: criar review (uma por utilizador/isca), agregado e voto "útil".
/// Usa um `sub` próprio (X-Test-Sub) para não colidir com outras classes paralelas.
/// </summary>
public class ReviewsTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private const string Sub = "test-rev-sub-0001";
    private const string Slug = "isca-003";
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

    private async Task<Guid> SeedUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = await db.UserAuthProviders.Where(p => p.ProviderUid == Sub)
            .Select(p => (Guid?)p.UserId).FirstOrDefaultAsync() ?? Guid.Empty;
        if (userId == Guid.Empty)
        {
            var user = new User { Id = Guid.NewGuid(), Username = "rev_tester", Role = "user" };
            db.Users.Add(user);
            db.UserAuthProviders.Add(new UserAuthProvider
            { Id = Guid.NewGuid(), UserId = user.Id, Provider = "test", ProviderUid = Sub });
            await db.SaveChangesAsync();
            userId = user.Id;
        }
        // limpa reviews remanescentes deste utilizador
        var lureId = await db.Lures.Where(l => l.Slug == Slug).Select(l => l.Id).FirstAsync();
        db.LureReviews.RemoveRange(db.LureReviews.Where(r => r.UserId == userId && r.LureId == lureId));
        await db.SaveChangesAsync();
        return userId;
    }

    private async Task CleanupAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.LureReviews.RemoveRange(db.LureReviews.Where(r => r.UserId == userId));
        db.UserAuthProviders.RemoveRange(db.UserAuthProviders.Where(p => p.UserId == userId));
        db.Users.RemoveRange(db.Users.Where(u => u.Id == userId));
        await db.SaveChangesAsync();
    }

    private record ReviewDto(Guid Id, int Rating, string? Body, int HelpfulCount, bool? IsHelpful);
    private record Aggregate(double? AvgRating, int TotalReviews, Dictionary<string, int> Distribution);
    private record ListResp(List<ReviewDto> Data, Aggregate Aggregate);
    private record Helpful(int HelpfulCount, bool IsHelpful);

    [Fact]
    public async Task Create_is_unique_aggregate_and_helpful_toggle()
    {
        var userId = await SeedUserAsync();
        try
        {
            var client = Client();

            // create
            var create = await client.PostAsJsonAsync($"/v1/lures/{Slug}/reviews", new { rating = 4, body = "Boa isca" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var review = await create.Content.ReadFromJsonAsync<ReviewDto>(Json);
            Assert.Equal(4, review!.Rating);

            // uma por utilizador/isca → 409
            var dup = await client.PostAsJsonAsync($"/v1/lures/{Slug}/reviews", new { rating = 5 });
            Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

            // list + agregado
            var list = await client.GetFromJsonAsync<ListResp>($"/v1/lures/{Slug}/reviews", Json);
            Assert.Contains(list!.Data, r => r.Id == review.Id);
            Assert.Equal(4.0, list.Aggregate.AvgRating);
            Assert.True(list.Aggregate.TotalReviews >= 1);
            Assert.Equal(1, list.Aggregate.Distribution["4"]);

            // voto útil: liga e desliga
            var on = await (await client.PostAsync($"/v1/reviews/{review.Id}/helpful", null)).Content.ReadFromJsonAsync<Helpful>(Json);
            Assert.True(on!.IsHelpful);
            Assert.Equal(1, on.HelpfulCount);
            var off = await (await client.PostAsync($"/v1/reviews/{review.Id}/helpful", null)).Content.ReadFromJsonAsync<Helpful>(Json);
            Assert.False(off!.IsHelpful);
            Assert.Equal(0, off.HelpfulCount);

            // delete
            var del = await client.DeleteAsync($"/v1/lures/{Slug}/reviews/{review.Id}");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        }
        finally
        {
            await CleanupAsync(userId);
        }
    }
}
