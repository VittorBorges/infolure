using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Infolure.Api.Features.Catalog;

namespace Infolure.IntegrationTests.Catalog;

/// <summary>
/// Testes de contrato/integração do read-path (T020, T021, T030, T037).
/// Cobrem US-01 (lista/filtro/sort), US-02 (busca/autocomplete) e US-03 (detalhe/404).
/// </summary>
public class CatalogEndpointsTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };
    private readonly HttpClient _client = factory.CreateClient();

    // T020 — contrato listLures
    [Fact]
    public async Task ListLures_returns_data_meta_and_facets()
    {
        var res = await _client.GetAsync("/v1/lures?per_page=5");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<LureListResponse>(Json);
        Assert.NotNull(body);
        Assert.True(body!.Meta.Total > 0);
        Assert.NotEmpty(body.Data);
        Assert.NotEmpty(body.Meta.Facets.LureTypes);
        Assert.True(res.Headers.Contains("X-Correlation-Id"));
    }

    // T021 — filtro por tipo devolve só esse tipo
    [Fact]
    public async Task ListLures_filtered_by_type_returns_only_that_type()
    {
        var body = await _client.GetFromJsonAsync<LureListResponse>("/v1/lures?lure_type=jig&per_page=50", Json);
        Assert.NotNull(body);
        Assert.All(body!.Data, l => Assert.Equal("jig", l.LureType));
    }

    // T021 — ordenação aceite
    [Fact]
    public async Task ListLures_accepts_newest_sort()
    {
        var res = await _client.GetAsync("/v1/lures?sort=newest&per_page=3");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    // T030 — autocomplete: ≤ 8, e < 2 chars devolve vazio
    [Fact]
    public async Task Suggest_returns_up_to_8_and_empty_for_short_query()
    {
        var ok = await _client.GetFromJsonAsync<SuggestResponse>("/v1/lures/suggest?q=Is", Json);
        Assert.NotNull(ok);
        Assert.InRange(ok!.Suggestions.Count, 0, 8);

        var empty = await _client.GetFromJsonAsync<SuggestResponse>("/v1/lures/suggest?q=I", Json);
        Assert.NotNull(empty);
        Assert.Empty(empty!.Suggestions);
    }

    // T037 — detalhe por slug
    [Fact]
    public async Task GetLure_returns_detail_for_seeded_slug()
    {
        var body = await _client.GetFromJsonAsync<LureDetailDto>("/v1/lures/isca-001", Json);
        Assert.NotNull(body);
        Assert.Equal("isca-001", body!.Slug);
        Assert.False(string.IsNullOrEmpty(body.Name));
    }

    // T037 — 404 para slug inexistente
    [Fact]
    public async Task GetLure_returns_404_for_unknown_slug()
    {
        var res = await _client.GetAsync("/v1/lures/nao-existe-xyz");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
