namespace Infolure.Api.Features.Admin;

// DTOs do backoffice (US admin — T078..T081).

public record CreateBrandRequest(string Slug, string Name);
public record CreateSpeciesRequest(string Slug, string? WaterType, string CommonName);
public record AddRetailerPriceRequest(string Retailer, string? Url, decimal PriceEur, bool InStock = true);

// Feature 006 — CRUD de marcas (update; create reutiliza CreateBrandRequest).
public record BrandWriteRequest(string? Slug, string Name);
public record BrandDetailDto(Guid Id, string Slug, string Name);

// ===================== Feature 005/006 — escrita completa de iscas =====================
// Payload partilhado por POST (criar) e PUT (editar). snake_case na serialização.
// Feature 006: "sizes"→"configurations" (+anzol por configuração); cor: photo_url→photo_urls[];
// remove HookSize/HookType/HookCount e IsIndexable ao nível da isca.

public record ConfigurationInput(
    string? Code, string Label, decimal? LengthMm, decimal WeightG,
    string? HookSize, string? HookType, short? HookCount, short SortOrder = 0);
public record HexCodeInput(string Hex, string? Label, short SortOrder = 0);
public record ColorInput(string? NamePt, string? NameEn, string? Pattern, List<string>? PhotoUrls, List<HexCodeInput>? HexCodes);
public record TargetSpeciesInput(Guid SpeciesId, string? Confidence);

public record LureWriteRequest(
    string Slug,
    string Name,
    string? Description,
    Guid? BrandId,
    string LureType,
    string? WaterType,
    string? ModelRef,
    string? Material,
    decimal? DepthMinM,
    decimal? DepthMaxM,
    string? Status,
    List<ConfigurationInput>? Configurations,
    List<ColorInput>? Colors,
    List<TargetSpeciesInput>? TargetSpecies);

// Projeção para edição (GET /v1/admin/lures/{id}) — superset de LureWriteRequest.
public record AdminLureConfigurationDto(
    Guid Id, string? Code, string Label, decimal? LengthMm, decimal WeightG,
    string? HookSize, string? HookType, short? HookCount, short SortOrder);
public record AdminLureHexCodeDto(string Hex, string? Label, short SortOrder);
public record AdminLureColorDto(Guid Id, string NamePt, string? NameEn, string? Pattern, IReadOnlyList<string> PhotoUrls, IReadOnlyList<AdminLureHexCodeDto> HexCodes);
public record AdminLureDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    Guid? BrandId,
    string? BrandName,                 // Feature 006 — pré-preenche o BrandPicker pelo nome
    string LureType,
    string? WaterType,
    string? ModelRef,
    string? Material,
    decimal? DepthMinM,
    decimal? DepthMaxM,
    string Status,
    IReadOnlyList<AdminLureConfigurationDto> Configurations,
    IReadOnlyList<AdminLureColorDto> Colors,
    IReadOnlyList<TargetSpeciesInput> TargetSpecies);
public record PriceSummary(decimal? Price6mMinEur, decimal? Price6mMaxEur, decimal? Price6mAvgEur);
public record ModerateReviewRequest(string Status);
