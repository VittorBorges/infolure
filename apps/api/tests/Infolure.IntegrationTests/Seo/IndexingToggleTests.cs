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

    // Feature 006 (US1) — o controlo de indexação POR ISCA foi removido; só existe o global.
    [Fact]
    public async Task Per_lure_indexable_endpoint_no_longer_exists()
    {
        var admin = _factory.AdminClient();
        var res = await admin.PutAsJsonAsync($"/v1/admin/lures/{Guid.NewGuid()}/indexable", new { is_indexable = false });
        Assert.True(res.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.MethodNotAllowed,
            $"esperado 404/405, veio {(int)res.StatusCode}");
    }

    // Feature 006 (US1) — GET admin do estado global.
    [Fact]
    public async Task Admin_can_read_global_indexing_state()
    {
        var admin = _factory.AdminClient();
        try
        {
            await admin.PutAsJsonAsync("/v1/admin/settings/indexing", new { enabled = false });
            var s = await admin.GetFromJsonAsync<JsonElement>("/v1/admin/settings/indexing");
            Assert.False(s.GetProperty("enabled").GetBoolean());
        }
        finally
        {
            await admin.PutAsJsonAsync("/v1/admin/settings/indexing", new { enabled = true });
        }
    }
}
