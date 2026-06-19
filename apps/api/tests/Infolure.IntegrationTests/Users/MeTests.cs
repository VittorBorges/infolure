using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infolure.IntegrationTests.Users;

using Infolure.IntegrationTests.Admin;

/// <summary>
/// Feature 007 — GET /v1/me: identidade da sessão atual para o painel admin.
/// Cobre o contrato contracts/me-api.yaml (200 autenticado com função, sem id; 401 sem perfil).
/// </summary>
public class MeTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private readonly AuthenticatedApiFactory _factory = factory;

    [Fact]
    public async Task Me_returns_identity_with_role_and_no_uuid()
    {
        var admin = _factory.AdminClient(); // cria user role=admin, username=admin_tester (sub via header)

        var res = await admin.GetAsync("/v1/me");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("admin", json.GetProperty("role").GetString());
        Assert.Equal("admin_tester", json.GetProperty("username").GetString());
        // FR-004 / SC-005 — nunca expor o identificador interno.
        Assert.False(json.TryGetProperty("id", out _), "MeDto não deve expor o id (UUID)");
    }

    [Fact]
    public async Task Me_without_resolvable_profile_returns_401()
    {
        // Autenticado (TestAuthHandler) mas com um sub que não corresponde a nenhum utilizador.
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", $"sub-inexistente-{Guid.NewGuid():N}");

        var res = await client.GetAsync("/v1/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
