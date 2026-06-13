namespace Infolure.Api.Features.Admin;

// DTOs do backoffice (US admin — T078..T081).

public record CreateBrandRequest(string Slug, string Name);
public record CreateSpeciesRequest(string Slug, string? WaterType, string CommonName);
public record CreateLureRequest(string Slug, string LureType, Guid? BrandId, string Name, string? Status);
public record UpdateLureRequest(string? Status, decimal? WeightG);
public record AddRetailerPriceRequest(string Retailer, string? Url, decimal PriceEur, bool InStock = true);
public record PriceSummary(decimal? Price6mMinEur, decimal? Price6mMaxEur, decimal? Price6mAvgEur);
public record ModerateReviewRequest(string Status);
