namespace Infolure.Api.Features.Catalog;

// DTOs espelhando contracts/api.yaml (Princípio III). snake_case na serialização
// é configurado globalmente (JsonNamingPolicy.SnakeCaseLower) no Program.cs.

public record LureCardDto(
    Guid Id,
    string Slug,
    string Name,
    string? Brand,
    string LureType,
    string? WaterType,
    decimal? WeightG,
    string? PrimaryImageUrl,
    string? PrimaryColorHex,
    string[] TargetSpecies,
    decimal? PriceAvgEur,
    int FavoritesCount,
    bool? IsFavorited);

public record FacetValue(string Value, int Count);

public record CatalogFacets(
    IReadOnlyList<FacetValue> LureTypes,
    IReadOnlyList<FacetValue> Brands,
    IReadOnlyList<FacetValue> WaterTypes,
    IReadOnlyList<FacetValue> Species);

public record ListMeta(int Total, int Page, int PerPage, CatalogFacets Facets);

public record LureListResponse(IReadOnlyList<LureCardDto> Data, ListMeta Meta);

public record SuggestItem(string Slug, string Name, string? Brand, string Type);

public record SuggestResponse(IReadOnlyList<SuggestItem> Suggestions);

// Filtros de query da listagem (US-01/US-02).
public record CatalogQuery
{
    public string? Q { get; init; }
    public string? LureType { get; init; }
    public string? WaterType { get; init; }
    public string? Species { get; init; }
    public string? Brand { get; init; }
    public double? WeightMin { get; init; }
    public double? WeightMax { get; init; }
    public double? DepthMin { get; init; }
    public double? DepthMax { get; init; }
    public string Sort { get; init; } = "popularity";
    public int Page { get; init; } = 1;
    public int PerPage { get; init; } = 20;
    public string Locale { get; init; } = "pt";
}

// ---- Detalhe (US-03) ----

// Feature 005 — hex_primary/hex_secondary mantêm-se como derivados (1º/2º hex) para compat. da
// leitura pública; hex_codes é a lista aberta completa. sizes expõe a lista de tamanhos.
public record LureHexCodeDto(string Hex, string? Label);
public record LureColorDto(Guid Id, string Name, string? HexPrimary, string? HexSecondary, string? Pattern, IReadOnlyList<LureHexCodeDto> HexCodes);
public record LureSizeDto(Guid Id, string? Code, string Label, decimal? LengthMm, decimal WeightG);
public record LureImageDto(string Url, Guid? ColorId, bool IsPrimary);
public record TargetSpeciesDto(string Slug, string CommonName, string? Confidence);
public record RetailerPriceDto(string Retailer, string? Url, decimal PriceEur, bool InStock);
public record PricingDto(
    decimal? AvgEur, decimal? MinEur, decimal? MaxEur,
    DateTimeOffset? UpdatedAt, IReadOnlyList<RetailerPriceDto> Retailers);

public record LureDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? Brand,
    string LureType,
    string? WaterType,
    decimal? WeightG,
    string? PrimaryImageUrl,
    string? PrimaryColorHex,
    string[] TargetSpecies,
    decimal? PriceAvgEur,
    int FavoritesCount,
    bool? IsFavorited,
    // campos do detalhe
    string? Description,
    decimal? LengthMm,
    decimal? DepthMinM,
    decimal? DepthMaxM,
    string? HookSize,
    string? HookType,
    short? HookCount,
    string? Material,
    IReadOnlyList<LureSizeDto> Sizes,
    IReadOnlyList<LureColorDto> Colors,
    IReadOnlyList<LureImageDto> Images,
    IReadOnlyList<TargetSpeciesDto> TargetSpeciesDetail,
    PricingDto? Pricing,
    double? AvgRating,
    int ReviewsCount,
    bool? IsInInventory);
