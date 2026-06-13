using System.Text.Json.Serialization;

namespace Infolure.Api.Infrastructure.Search;

/// <summary>
/// Documento indexado no Typesense (coleção "lures"). Espelha o schema em data-model.md.
/// popularity_score = nº de favoritos + nº de entradas de inventário (job noturno).
/// Os nomes JSON são snake_case para casar com o schema da coleção.
/// </summary>
public class LureSearchDocument
{
    [JsonPropertyName("id")] public string Id { get; set; } = null!;
    [JsonPropertyName("slug")] public string Slug { get; set; } = null!;
    [JsonPropertyName("model_ref")] public string? ModelRef { get; set; }
    [JsonPropertyName("name_pt")] public string NamePt { get; set; } = null!;
    [JsonPropertyName("name_en")] public string? NameEn { get; set; }
    [JsonPropertyName("name_es")] public string? NameEs { get; set; }
    [JsonPropertyName("brand_name")] public string BrandName { get; set; } = "";
    [JsonPropertyName("lure_type")] public string LureType { get; set; } = "";
    [JsonPropertyName("water_type")] public string WaterType { get; set; } = "";
    [JsonPropertyName("weight_g")] public float? WeightG { get; set; }
    [JsonPropertyName("depth_min_m")] public float? DepthMinM { get; set; }
    [JsonPropertyName("depth_max_m")] public float? DepthMaxM { get; set; }
    [JsonPropertyName("target_species")] public string[] TargetSpecies { get; set; } = [];
    [JsonPropertyName("price_6m_avg_eur")] public float? Price6mAvgEur { get; set; }
    [JsonPropertyName("primary_image_url")] public string? PrimaryImageUrl { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "draft";
    [JsonPropertyName("popularity_score")] public int PopularityScore { get; set; }
    [JsonPropertyName("favorites_count")] public int FavoritesCount { get; set; }
    [JsonPropertyName("created_at")] public long CreatedAt { get; set; }
}
