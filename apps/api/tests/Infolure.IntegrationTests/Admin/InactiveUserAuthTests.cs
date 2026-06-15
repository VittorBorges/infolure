using System.Net;
using System.Net.Http.Json;

namespace Infolure.IntegrationTests.Admin;

/// <summary>T023 / FR-013a: desativar um utilizador bloqueia a autenticação de imediato (401).</summary>
public class InactiveUserAuthTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private readonly AuthenticatedApiFactory _factory = factory;

    [Fact]
    public async Task Deactivated_user_cannot_authenticate()
    {
        var (user, _, userId) = _factory.UserClient("inact1");
        var admin = _factory.AdminClient();
        try
        {
            // ativo: requisição autenticada funciona
            Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/v1/me/favorites")).StatusCode);

            // admin desativa → cache invalidado
            Assert.Equal(HttpStatusCode.NoContent,
                (await admin.PutAsJsonAsync($"/v1/admin/users/{userId}/active", new { is_active = false })).StatusCode);

            // próxima requisição do utilizador → 401
            Assert.Equal(HttpStatusCode.Unauthorized, (await user.GetAsync("/v1/me/favorites")).StatusCode);
        }
        finally
        {
            await _factory.HardDeleteUserAsync(userId);
        }
    }
}
