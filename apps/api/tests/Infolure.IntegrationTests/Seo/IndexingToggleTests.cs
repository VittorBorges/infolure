using System.Net.Http.Json;
using System.Text.Json;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.IntegrationTests.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Seo;

/// <summary>
/// T037 (US-03): o interruptor global e o is_indexable por isca refletem-se em GET /v1/seo
/// (flag + sitemap). A cache é invalidada na escrita.
/// </summary>
public class IndexingToggleTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private readonly AuthenticatedApiFactory _factory = factory;

    private static List<string> Slugs(JsonElement seo) =>
        seo.GetProperty("sitemap").EnumerateArray().Select(e => e.GetProperty("slug").GetString()!).ToList();

    [Fact]
    public async Task Global_toggle_reflects_in_seo()
    {
        var admin = _factory.AdminClient();
        var anon = _factory.CreateClient();
        try
        {
            await admin.PutAsJsonAsync("/v1/admin/settings/indexing", new { enabled = true });
            var on = await anon.GetFromJsonAsync<JsonElement>("/v1/seo");
            Assert.True(on.GetProperty("indexing_enabled").GetBoolean());
            Assert.True(Slugs(on).Count > 0); // catálogo semeado elegível

            await admin.PutAsJsonAsync("/v1/admin/settings/indexing", new { enabled = false });
            var off = await anon.GetFromJsonAsync<JsonElement>("/v1/seo");
            Assert.False(off.GetProperty("indexing_enabled").GetBoolean());
            Assert.Empty(Slugs(off)); // sitemap vazio quando desligado
        }
        finally
        {
            await admin.PutAsJsonAsync("/v1/admin/settings/indexing", new { enabled = true });
        }
    }

    [Fact]
    public async Task Per_lure_indexable_controls_sitemap_membership()
    {
        var admin = _factory.AdminClient();
        var anon = _factory.CreateClient();
        await admin.PutAsJsonAsync("/v1/admin/settings/indexing", new { enabled = true });

        const string slug = "t-seo-lure-1";
        var created = await admin.PostAsJsonAsync("/v1/admin/lures",
            new { slug, name = "SEO Lure", lure_type = "jig", status = "published" });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        try
        {
            Assert.Contains(slug, Slugs(await anon.GetFromJsonAsync<JsonElement>("/v1/seo")));

            await admin.PutAsJsonAsync($"/v1/admin/lures/{id}/indexable", new { is_indexable = false });
            Assert.DoesNotContain(slug, Slugs(await anon.GetFromJsonAsync<JsonElement>("/v1/seo")));
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Lures.IgnoreQueryFilters().Where(l => l.Id == id).ExecuteDeleteAsync();
        }
    }
}
