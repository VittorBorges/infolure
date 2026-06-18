using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Infolure.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Admin;

/// <summary>
/// T021 / FR-009/FR-010/FR-011: CRUD por recurso (list/filtro/paginação, soft-delete, restore,
/// toggle active) com efeitos colaterais (reindex Typesense em lures).
/// </summary>
public class AdminCrudTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private readonly AuthenticatedApiFactory _factory = factory;

    [Fact]
    public async Task Lure_lifecycle_via_admin_and_public_visibility()
    {
        var admin = _factory.AdminClient();
        const string slug = "t-crud-lure-1";

        // create (published) → indexável/visível
        var created = await admin.PostAsJsonAsync("/v1/admin/lures",
            new { slug, name = "T Crud Lure", lure_type = "jig", status = "published", sizes = new[] { new { label = "STD", weight_g = 10 } } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        try
        {
            // list com filtro
            var listRaw = await admin.GetStringAsync($"/v1/admin/lures?q={slug}");
            Assert.Contains(slug, listRaw);

            // desativar → some do público (reindex remove + inativa)
            Assert.Equal(HttpStatusCode.NoContent,
                (await admin.PutAsJsonAsync($"/v1/admin/lures/{id}/active", new { is_active = false })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _factory.CreateClient().GetAsync($"/v1/lures/{slug}")).StatusCode);

            // reativar → volta a ser público
            await admin.PutAsJsonAsync($"/v1/admin/lures/{id}/active", new { is_active = true });
            Assert.Equal(HttpStatusCode.OK, (await _factory.CreateClient().GetAsync($"/v1/lures/{slug}")).StatusCode);

            // soft-delete → some; aparece com include=deleted
            Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/v1/admin/lures/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _factory.CreateClient().GetAsync($"/v1/lures/{slug}")).StatusCode);
            var deletedList = await admin.GetStringAsync($"/v1/admin/lures?include=deleted&q={slug}");
            Assert.Contains(slug, deletedList);

            // restore → volta
            Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/v1/admin/lures/{id}/restore", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await _factory.CreateClient().GetAsync($"/v1/lures/{slug}")).StatusCode);
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Lures.IgnoreQueryFilters().Where(l => l.Id == id).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task Users_list_is_paged()
    {
        var admin = _factory.AdminClient();
        var raw = await admin.GetFromJsonAsync<JsonElement>("/v1/admin/users?per_page=5");
        Assert.True(raw.TryGetProperty("meta", out var meta));
        Assert.True(meta.TryGetProperty("per_page", out var per));
        Assert.Equal(5, per.GetInt32());
    }
}
