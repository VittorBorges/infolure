using Infolure.Api.Infrastructure.Search;
using Typesense;

namespace Infolure.Api.Features.Catalog;

/// <summary>
/// Autocomplete (US-02): instant search no Typesense, limitado a 8 sugestões.
/// </summary>
public class SuggestService(ITypesenseClient client)
{
    private const string QueryByFields = "name_pt,name_en,name_es,brand_name,model_ref";

    public async Task<SuggestResponse> SuggestAsync(string q, string locale, CancellationToken ct = default)
    {
        var sp = new SearchParameters(q, QueryByFields)
        {
            FilterBy = "status:=published",
            Page = 1,
            PerPage = 8,
        };

        var result = await client.Search<LureSearchDocument>(TypesenseExtensions.CollectionName, sp);

        var suggestions = result.Hits.Select(h =>
        {
            var d = h.Document;
            var name = locale switch
            {
                "en" => d.NameEn ?? d.NamePt,
                "es" => d.NameEs ?? d.NamePt,
                _ => d.NamePt,
            };
            return new SuggestItem(d.Slug, name, d.BrandName, d.LureType);
        }).Take(8).ToList();

        return new SuggestResponse(suggestions);
    }
}
