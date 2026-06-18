namespace Infolure.Api.Features.Admin;

// DTOs do backoffice (US admin — T078..T081).

public record CreateBrandRequest(string Slug, string Name);
public record CreateSpeciesRequest(string Slug, string? WaterType, string CommonName);
public record AddRetailerPriceRequest(string Retailer, string? Url, decimal PriceEur, bool InStock = true);

// ===================== Feature 005 — escrita completa de iscas =====================
// Payload partilhado por POST (criar) e PUT (editar). snake_case na serialização.

public record SizeInput(string? Code, string Label, decimal? LengthMm, decimal WeightG, short SortOrder = 0);
public record HexCodeInput(string Hex, string? Label, short SortOrder = 0);
public record ColorInput(string? NamePt, string? NameEn, string? Pattern, string? PhotoUrl, List<HexCodeInput>? HexCodes);
public record TargetSpeciesInput(Guid SpeciesId, string? Confidence);

public record LureWriteRequest(
    string Slug,
    string Name,
    string? Description,
    Guid? BrandId,
    string LureType,
    string? WaterType,
    string? ModelRef,
    string? HookSize,
    string? HookType,
    short? HookCount,
    string? Material,
    decimal? DepthMinM,
    decimal? DepthMaxM,
    string? Status,
    List<SizeInput>? Sizes,
    List<ColorInput>? Colors,
    List<TargetSpeciesInput>? TargetSpecies);

// Projeção para edição (GET /v1/admin/lures/{id}) — superset de LureWriteRequest.
public record AdminLureSizeDto(Guid Id, string? Code, string Label, decimal? LengthMm, decimal WeightG, short SortOrder);
public record AdminLureHexCodeDto(string Hex, string? Label, short SortOrder);
public record AdminLureColorDto(Guid Id, string NamePt, string? NameEn, string? Pattern, string? PhotoUrl, IReadOnlyList<AdminLureHexCodeDto> HexCodes);
public record AdminLureDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    Guid? BrandId,
    string LureType,
    string? WaterType,
    string? ModelRef,
    string? HookSize,
    string? HookType,
    short? HookCount,
    string? Material,
    decimal? DepthMinM,
    decimal? DepthMaxM,
    string Status,
    bool IsIndexable,
    IReadOnlyList<AdminLureSizeDto> Sizes,
    IReadOnlyList<AdminLureColorDto> Colors,
    IReadOnlyList<TargetSpeciesInput> TargetSpecies);
public record PriceSummary(decimal? Price6mMinEur, decimal? Price6mMaxEur, decimal? Price6mAvgEur);
public record ModerateReviewRequest(string Status);
