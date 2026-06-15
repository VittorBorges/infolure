using System.Net.Http.Json;
using System.Text.Json;

namespace Infolure.IntegrationTests.Admin;

/// <summary>T024 / FR-008: o dashboard devolve as métricas esperadas.</summary>
public class DashboardTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private readonly AuthenticatedApiFactory _factory = factory;

    [Fact]
    public async Task Dashboard_has_expected_shape()
    {
        var d = await _factory.AdminClient().GetFromJsonAsync<JsonElement>("/v1/admin/dashboard");

        Assert.True(d.TryGetProperty("users", out var users));
        Assert.True(users.TryGetProperty("total", out _));
        Assert.True(users.TryGetProperty("new_7d", out _));

        Assert.True(d.TryGetProperty("lures", out var lures));
        Assert.True(lures.TryGetProperty("by_status", out _));
        Assert.True(lures.TryGetProperty("by_source", out var bySource));
        Assert.True(lures.GetProperty("active").GetInt32() >= 1); // pelo menos as iscas semeadas
        Assert.True(bySource.TryGetProperty("automation", out _)); // seed → automation

        Assert.True(d.TryGetProperty("reviews", out var reviews));
        Assert.True(reviews.TryGetProperty("pending", out _));
        Assert.True(d.TryGetProperty("favorites", out _));
        Assert.True(d.TryGetProperty("inventory", out _));
    }
}
