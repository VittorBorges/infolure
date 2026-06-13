using System.Globalization;
using Infolure.Api.Infrastructure.Search;
using Typesense;

namespace Infolure.Api.Features.Catalog;

/// <summary>
/// Listagem/busca de iscas via Typesense (US-01/US-02): filtros, facets, ordenação e paginação.
/// Campos de busca: name_*, brand_name, model_ref (inclui referência de modelo — US-02/C2).
/// </summary>
public class LureListService(ITypesenseClient client, ILogger<LureListService> logger)
{
    private const string QueryByFields = "name_pt,name_en,name_es,brand_name,model_ref";

    public async Task<LureListResponse> SearchAsync(CatalogQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var perPage = Math.Clamp(q.PerPage, 1, 50);
        var text = string.IsNullOrWhiteSpace(q.Q) ? "*" : q.Q.Trim();

        var sp = new SearchParameters(text, QueryByFields)
        {
            FilterBy = BuildFilter(q),
            FacetBy = "lure_type,brand_name,water_type,target_species",
            SortBy = MapSort(q.Sort),
            Page = page,
            PerPage = perPage,
        };

        var result = await client.Search<LureSearchDocument>(TypesenseExtensions.CollectionName, sp);

        var nameSelector = NameSelector(q.Locale);
        var data = result.Hits.Select(h =>
        {
            var d = h.Document;
            return new LureCardDto(
                Id: Guid.Parse(d.Id),
                Slug: d.Slug,
                Name: nameSelector(d),
                Brand: d.BrandName,
                LureType: d.LureType,
                WaterType: d.WaterType,
                WeightG: d.WeightG is null ? null : (decimal)d.WeightG.Value,
                PrimaryImageUrl: d.PrimaryImageUrl,
                PrimaryColorHex: null,
                TargetSpecies: d.TargetSpecies,
                PriceAvgEur: d.Price6mAvgEur is null ? null : (decimal)d.Price6mAvgEur.Value,
                FavoritesCount: d.FavoritesCount,
                IsFavorited: null); // preenchido quando autenticado (US-05)
        }).ToList();

        var facets = MapFacets(result);
        var meta = new ListMeta(result.Found, page, perPage, facets);
        logger.LogInformation("Catalog search q={Query} found={Found} page={Page}", text, result.Found, page);
        return new LureListResponse(data, meta);
    }

    private static string BuildFilter(CatalogQuery q)
    {
        var clauses = new List<string> { "status:=published" };
        if (!string.IsNullOrWhiteSpace(q.LureType)) clauses.Add($"lure_type:=`{q.LureType}`");
        if (!string.IsNullOrWhiteSpace(q.WaterType)) clauses.Add($"water_type:=`{q.WaterType}`");
        if (!string.IsNullOrWhiteSpace(q.Brand)) clauses.Add($"brand_name:=`{q.Brand}`");
        if (!string.IsNullOrWhiteSpace(q.Species))
        {
            var slugs = q.Species.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            clauses.Add($"target_species:=[{string.Join(",", slugs.Select(s => $"`{s}`"))}]");
        }
        if (q.WeightMin is { } wmin) clauses.Add($"weight_g:>={F(wmin)}");
        if (q.WeightMax is { } wmax) clauses.Add($"weight_g:<={F(wmax)}");
        if (q.DepthMin is { } dmin) clauses.Add($"depth_min_m:>={F(dmin)}");
        if (q.DepthMax is { } dmax) clauses.Add($"depth_max_m:<={F(dmax)}");
        return string.Join(" && ", clauses);
    }

    private static string MapSort(string sort) => sort switch
    {
        "price_asc" => "price_6m_avg_eur:asc",
        "price_desc" => "price_6m_avg_eur:desc",
        "newest" => "created_at:desc",
        _ => "popularity_score:desc",
    };

    private static Func<LureSearchDocument, string> NameSelector(string locale) => locale switch
    {
        "en" => d => d.NameEn ?? d.NamePt,
        "es" => d => d.NameEs ?? d.NamePt,
        _ => d => d.NamePt,
    };

    private static CatalogFacets MapFacets(SearchResult<LureSearchDocument> result)
    {
        IReadOnlyList<FacetValue> Get(string field) =>
            result.FacetCounts?.FirstOrDefault(f => f.FieldName == field)?.Counts
                .Select(c => new FacetValue(c.Value, c.Count)).ToList() ?? [];

        return new CatalogFacets(
            LureTypes: Get("lure_type"),
            Brands: Get("brand_name"),
            WaterTypes: Get("water_type"),
            Species: Get("target_species"));
    }

    private static string F(double v) => v.ToString(CultureInfo.InvariantCulture);
}
