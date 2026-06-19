using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infolure.IntegrationTests.Admin;

/// <summary>
/// Feature 006 (US2) — CRUD de marcas no backoffice: criar, obter, editar e (soft-)eliminar.
/// </summary>
public class BrandCrudTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private readonly AuthenticatedApiFactory _factory = factory;

    [Fact]
    public async Task Create_get_update_and_delete_brand()
    {
        var admin = _factory.AdminClient();
        var slug = $"t-brand-{Guid.NewGuid():N}";

        // create
        var created = await admin.PostAsJsonAsync("/v1/admin/brands", new { slug, name = "Marca Teste" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // get
        var got = await admin.GetFromJsonAsync<JsonElement>($"/v1/admin/brands/{id}");
        Assert.Equal("Marca Teste", got.GetProperty("name").GetString());
        Assert.Equal(slug, got.GetProperty("slug").GetString());

        // update (nome)
        var put = await admin.PutAsJsonAsync($"/v1/admin/brands/{id}", new { slug, name = "Marca Editada" });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
        var after = await admin.GetFromJsonAsync<JsonElement>($"/v1/admin/brands/{id}");
        Assert.Equal("Marca Editada", after.GetProperty("name").GetString());

        // listável e pesquisável por nome (US3)
        var list = await admin.GetStringAsync("/v1/admin/brands?q=Editada");
        Assert.Contains(id.ToString(), list);

        // delete (soft)
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/v1/admin/brands/{id}")).StatusCode);
    }

    [Fact]
    public async Task Update_missing_brand_returns_404()
    {
        var admin = _factory.AdminClient();
        var res = await admin.PutAsJsonAsync($"/v1/admin/brands/{Guid.NewGuid()}", new { name = "X" });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
