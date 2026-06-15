using System.Net;
using System.Net.Http.Json;

namespace Infolure.IntegrationTests.Admin;

/// <summary>T022 / FR-013: bloqueios — não desativar a própria conta; permitir desativar outro admin
/// quando não é o último.</summary>
public class AdminSafeguardsTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private readonly AuthenticatedApiFactory _factory = factory;

    [Fact]
    public async Task Cannot_deactivate_or_delete_self()
    {
        var admin = _factory.AdminClient();
        var selfId = _factory.EnsureUser(AdminTestHelpers.AdminSub, "admin_tester", "admin");

        var deact = await admin.PutAsJsonAsync($"/v1/admin/users/{selfId}/active", new { is_active = false });
        Assert.Equal(HttpStatusCode.Conflict, deact.StatusCode);

        var del = await admin.DeleteAsync($"/v1/admin/users/{selfId}");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
    }

    [Fact]
    public async Task Can_deactivate_another_admin_when_not_last()
    {
        var admin = _factory.AdminClient(); // garante admin_tester (actor)
        var otherId = _factory.EnsureUser("test-admin-2", "admin_tester_2", "admin");
        try
        {
            var res = await admin.PutAsJsonAsync($"/v1/admin/users/{otherId}/active", new { is_active = false });
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode); // 2 admins → não é o último
        }
        finally
        {
            await _factory.HardDeleteUserAsync(otherId);
        }
    }
}
