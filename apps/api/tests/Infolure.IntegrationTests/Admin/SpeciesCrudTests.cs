using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infolure.IntegrationTests.Admin;

/// <summary>
/// Feature 006 — CRUD de espécies no backoffice: criar, obter, editar e (soft-)eliminar.
/// Espelha BrandCrudTests; cobre ainda o tipo de água/família e a pesquisa por nome.
/// </summary>
public class SpeciesCrudTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private readonly AuthenticatedApiFactory _factory = factory;

    [Fact]
    public async Task Create_get_update_and_delete_species()
    {
        var admin = _factory.AdminClient();
        var slug = $"t-species-{Guid.NewGuid():N}";

        // create
        var created = await admin.PostAsJsonAsync("/v1/admin/species",
            new { slug, water_type = "saltwater", common_name = "Robalo", family = "Moronidae" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // get
        var got = await admin.GetFromJsonAsync<JsonElement>($"/v1/admin/species/{id}");
        Assert.Equal("Robalo", got.GetProperty("common_name").GetString());
        Assert.Equal(slug, got.GetProperty("slug").GetString());
        Assert.Equal("saltwater", got.GetProperty("water_type").GetString());

        // update (nome + tipo de água)
        var put = await admin.PutAsJsonAsync($"/v1/admin/species/{id}",
            new { slug, common_name = "Robalo-legítimo", water_type = "both", family = "Moronidae" });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
        var after = await admin.GetFromJsonAsync<JsonElement>($"/v1/admin/species/{id}");
        Assert.Equal("Robalo-legítimo", after.GetProperty("common_name").GetString());
        Assert.Equal("both", after.GetProperty("water_type").GetString());

        // listável e pesquisável por nome
        var list = await admin.GetStringAsync("/v1/admin/species?q=Robalo-leg");
        Assert.Contains(id.ToString(), list);

        // delete (soft)
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/v1/admin/species/{id}")).StatusCode);
    }

    [Fact]
    public async Task Update_missing_species_returns_404()
    {
        var admin = _factory.AdminClient();
        var res = await admin.PutAsJsonAsync($"/v1/admin/species/{Guid.NewGuid()}",
            new { common_name = "X" });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
