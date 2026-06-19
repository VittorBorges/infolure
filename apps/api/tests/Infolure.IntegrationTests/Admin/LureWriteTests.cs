using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infolure.IntegrationTests.Admin;

/// <summary>
/// Feature 005 (US1/US2/US3): registo/edição completos de iscas — lista de tamanhos, descrição,
/// cores com lista aberta de hex, validação de hex e preservação de campos não alterados.
/// </summary>
public class LureWriteTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private readonly AuthenticatedApiFactory _factory = factory;

    // US1 — criar isca com vários tamanhos + descrição (FR-001/003/004).
    [Fact]
    public async Task Create_lure_with_sizes_and_description()
    {
        var admin = _factory.AdminClient();
        var slug = $"t-f005-create-{Guid.NewGuid():N}";
        var body = new
        {
            slug, name = "Minnow 90", description = "Isca de teste 005", lure_type = "jerkbait",
            status = "published",
            configurations = new[]
            {
                new { label = "90SP", length_mm = 90, weight_g = 9.5, sort_order = 0 },
                new { label = "110SP", length_mm = 110, weight_g = 15.0, sort_order = 1 },
            },
        };
        var created = await admin.PostAsJsonAsync("/v1/admin/lures", body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var detail = await admin.GetFromJsonAsync<JsonElement>($"/v1/admin/lures/{id}");
        Assert.Equal("Minnow 90", detail.GetProperty("name").GetString());
        Assert.Equal("Isca de teste 005", detail.GetProperty("description").GetString());
        Assert.Equal(2, detail.GetProperty("configurations").GetArrayLength());
    }

    // US2 — editar preservando campos não alterados; status omisso não despromove (FR-013/SC-005).
    [Fact]
    public async Task Update_preserves_untouched_fields_and_status()
    {
        var admin = _factory.AdminClient();
        var slug = $"t-f005-edit-{Guid.NewGuid():N}";
        var create = await admin.PostAsJsonAsync("/v1/admin/lures", new
        {
            slug, name = "Original", description = "desc", lure_type = "jig", status = "published",
            configurations = new[] { new { label = "STD", weight_g = 10.0 } },
        });
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // PUT alterando só a descrição, SEM enviar status → deve preservar "published".
        var put = await admin.PutAsJsonAsync($"/v1/admin/lures/{id}", new
        {
            slug, name = "Original", description = "desc editada", lure_type = "jig",
            configurations = new[] { new { label = "STD", weight_g = 10.0 } },
        });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var detail = await admin.GetFromJsonAsync<JsonElement>($"/v1/admin/lures/{id}");
        Assert.Equal("desc editada", detail.GetProperty("description").GetString());
        Assert.Equal("published", detail.GetProperty("status").GetString());
        Assert.Single(detail.GetProperty("configurations").EnumerateArray());
    }

    // US3 — cor com múltiplos hex (incl. duplicado permitido) persiste; foto opcional ausente é válida.
    [Fact]
    public async Task Create_lure_with_multi_hex_color_no_photo()
    {
        var admin = _factory.AdminClient();
        var slug = $"t-f005-color-{Guid.NewGuid():N}";
        var create = await admin.PostAsJsonAsync("/v1/admin/lures", new
        {
            slug, name = "Tiger", lure_type = "crankbait", status = "draft",
            configurations = new[] { new { label = "STD", weight_g = 12.0 } },
            colors = new[]
            {
                new
                {
                    name_pt = "Tiger",
                    hex_codes = new[]
                    {
                        new { hex = "#00FF00", label = "verde" },
                        new { hex = "#ffff00", label = "amarelo" },
                        new { hex = "#00ff00", label = "verde mate" }, // duplicado permitido
                    },
                },
            },
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var detail = await admin.GetFromJsonAsync<JsonElement>($"/v1/admin/lures/{id}");
        var colors = detail.GetProperty("colors");
        Assert.Equal(1, colors.GetArrayLength());
        var hexes = colors[0].GetProperty("hex_codes");
        Assert.Equal(3, hexes.GetArrayLength());
        Assert.Equal("#00ff00", hexes[0].GetProperty("hex").GetString()); // normalizado p/ minúsculas
    }

    // US3 — hex inválido → 422 com mapa de erros (FR-009).
    [Fact]
    public async Task Invalid_hex_is_rejected_with_422()
    {
        var admin = _factory.AdminClient();
        var slug = $"t-f005-badhex-{Guid.NewGuid():N}";
        var res = await admin.PostAsJsonAsync("/v1/admin/lures", new
        {
            slug, name = "Bad", lure_type = "jig",
            configurations = new[] { new { label = "STD", weight_g = 10.0 } },
            colors = new[] { new { name_pt = "X", hex_codes = new[] { new { hex = "#12xz" } } } },
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    // US1 — sem tamanhos → 422 (FR-003).
    [Fact]
    public async Task Missing_sizes_is_rejected_with_422()
    {
        var admin = _factory.AdminClient();
        var res = await admin.PostAsJsonAsync("/v1/admin/lures", new
        {
            slug = $"t-f005-nosize-{Guid.NewGuid():N}", name = "NoSize", lure_type = "jig",
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    // Slug duplicado → 409.
    [Fact]
    public async Task Duplicate_slug_returns_409()
    {
        var admin = _factory.AdminClient();
        var slug = $"t-f005-dup-{Guid.NewGuid():N}";
        var b = new { slug, name = "Dup", lure_type = "jig", configurations = new[] { new { label = "STD", weight_g = 10.0 } } };
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/v1/admin/lures", b)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/v1/admin/lures", b)).StatusCode);
    }

    // US4 (F006) — anzol por configuração, distinto entre configurações, persiste.
    [Fact]
    public async Task Hook_data_is_per_configuration()
    {
        var admin = _factory.AdminClient();
        var slug = $"t-f006-hook-{Guid.NewGuid():N}";
        var create = await admin.PostAsJsonAsync("/v1/admin/lures", new
        {
            slug, name = "Hooks", lure_type = "jig", status = "draft",
            configurations = new[]
            {
                new { label = "S", weight_g = 8.0, hook_size = "#6", hook_type = "single", hook_count = 1, sort_order = 0 },
                new { label = "L", weight_g = 16.0, hook_size = "#2", hook_type = "treble", hook_count = 2, sort_order = 1 },
            },
        });
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var detail = await admin.GetFromJsonAsync<JsonElement>($"/v1/admin/lures/{id}");
        var cfgs = detail.GetProperty("configurations").EnumerateArray().ToList();
        Assert.Equal(2, cfgs.Count);
        Assert.Equal("#6", cfgs[0].GetProperty("hook_size").GetString());
        Assert.Equal("treble", cfgs[1].GetProperty("hook_type").GetString());
        Assert.Equal(2, cfgs[1].GetProperty("hook_count").GetInt32());
    }

    // US3 (F006) — o detalhe inclui brand_name; criar com brand_id persiste.
    [Fact]
    public async Task Detail_includes_brand_name()
    {
        var admin = _factory.AdminClient();
        var bslug = $"t-f006-brand-{Guid.NewGuid():N}";
        var brandRes = await admin.PostAsJsonAsync("/v1/admin/brands", new { slug = bslug, name = "Rapala Teste" });
        var brandId = (await brandRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var create = await admin.PostAsJsonAsync("/v1/admin/lures", new
        {
            slug = $"t-f006-lb-{Guid.NewGuid():N}", name = "Com Marca", lure_type = "jig", brand_id = brandId,
            configurations = new[] { new { label = "STD", weight_g = 10.0 } },
        });
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var detail = await admin.GetFromJsonAsync<JsonElement>($"/v1/admin/lures/{id}");
        Assert.Equal(brandId, detail.GetProperty("brand_id").GetGuid());
        Assert.Equal("Rapala Teste", detail.GetProperty("brand_name").GetString());
    }

    // US5 (F006) — uma cor pode ter várias fotos (photo_urls[]), preservadas e ordenadas.
    [Fact]
    public async Task Color_persists_multiple_photos()
    {
        var admin = _factory.AdminClient();
        var slug = $"t-f006-photos-{Guid.NewGuid():N}";
        var create = await admin.PostAsJsonAsync("/v1/admin/lures", new
        {
            slug, name = "Fotos", lure_type = "jig", status = "draft",
            configurations = new[] { new { label = "STD", weight_g = 10.0 } },
            colors = new[]
            {
                new
                {
                    name_pt = "Tiger",
                    photo_urls = new[] { "https://x/a.jpg", "https://x/b.jpg", "https://x/c.jpg" },
                    hex_codes = new[] { new { hex = "#00ff00", label = "verde" } },
                },
            },
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var detail = await admin.GetFromJsonAsync<JsonElement>($"/v1/admin/lures/{id}");
        var photos = detail.GetProperty("colors")[0].GetProperty("photo_urls").EnumerateArray().Select(p => p.GetString()).ToList();
        Assert.Equal(3, photos.Count);
        Assert.Equal("https://x/a.jpg", photos[0]);
    }
}
