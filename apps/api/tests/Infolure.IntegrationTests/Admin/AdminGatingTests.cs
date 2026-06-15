using System.Net;

namespace Infolure.IntegrationTests.Admin;

/// <summary>T020 / FR-007: o painel é restrito a admin; não-admin recebe 403.</summary>
public class AdminGatingTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private readonly AuthenticatedApiFactory _factory = factory;

    [Theory]
    [InlineData("/v1/admin/dashboard")]
    [InlineData("/v1/admin/lures")]
    [InlineData("/v1/admin/users")]
    public async Task NonAdmin_gets_403(string path)
    {
        var res = await _factory.CreateClient().GetAsync(path); // autenticado, sem role admin
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Admin_can_reach_dashboard()
    {
        var res = await _factory.AdminClient().GetAsync("/v1/admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
