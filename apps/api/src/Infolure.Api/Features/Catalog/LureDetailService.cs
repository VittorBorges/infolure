using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Features.Catalog;

/// <summary>
/// Detalhe de isca (US-03): carregado do Postgres (fonte de verdade) com cores, imagens,
/// espécies-alvo, preços de retalhistas e agregado de reviews. 404 se não publicada.
/// </summary>
public class LureDetailService(AppDbContext db)
{
    public async Task<LureDetailDto?> GetBySlugAsync(string slug, string locale, CancellationToken ct = default)
    {
        // Visibilidade pública (F002 US-01): publicada + ativa (DeletedAt tratado pelo global query
        // filter) + marca-pai ativa/não-eliminada (FR-003a). A espécie é relação fraca (FR-003b) →
        // não condiciona a visibilidade da isca, apenas é filtrada na projeção abaixo.
        var lure = await db.Lures
            .Where(l => l.Slug == slug && l.Status == "published" && l.IsActive)
            .Where(l => l.BrandId == null || db.Brands.Any(b => b.Id == l.BrandId && b.IsActive))
            .Include(l => l.Brand).ThenInclude(b => b!.Translations)
            .Include(l => l.Translations)
            .Include(l => l.Sizes)
            .Include(l => l.Colors)
            .Include(l => l.Images)
            .Include(l => l.TargetSpecies).ThenInclude(ts => ts.Species).ThenInclude(s => s.Translations)
            .Include(l => l.RetailerPrices)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (lure is null) return null;

        // FR-003b: descartar espécies-alvo inativas (eliminadas já saem pelo global query filter).
        var targetSpecies = lure.TargetSpecies.Where(ts => ts.Species is { IsActive: true }).ToList();

        var favoritesCount = await db.UserLureFavorites.CountAsync(f => f.LureId == lure.Id, ct);
        var reviews = await db.LureReviews
            .Where(r => r.LureId == lure.Id && r.Status == "published")
            .Select(r => r.Rating)
            .ToListAsync(ct);

        var name = Tr(lure.Translations, locale) ?? lure.Slug;
        var description = lure.Translations.FirstOrDefault(t => t.Locale == locale)?.Description
                          ?? lure.Translations.FirstOrDefault(t => t.Locale == "pt")?.Description;
        var brand = lure.Brand?.Translations.FirstOrDefault(t => t.Locale == "pt")?.Name;
        var primaryImage = lure.Images.FirstOrDefault(i => i.IsPrimary) ?? lure.Images.FirstOrDefault();

        // Feature 005 — peso/comprimento derivados da lista de tamanhos (fonte única).
        var orderedSizes = lure.Sizes.OrderBy(s => s.SortOrder).ToList();
        decimal? weightG = orderedSizes.Count > 0 ? orderedSizes.Min(s => s.WeightG) : null;
        decimal? lengthMm = orderedSizes.Where(s => s.LengthMm != null).Select(s => s.LengthMm).Min();
        var firstColor = lure.Colors.FirstOrDefault();

        var pricing = lure.RetailerPrices.Count == 0 && lure.Price6mAvgEur is null
            ? null
            : new PricingDto(
                AvgEur: lure.Price6mAvgEur,
                MinEur: lure.Price6mMinEur,
                MaxEur: lure.Price6mMaxEur,
                UpdatedAt: lure.Price6mUpdatedAt,
                Retailers: lure.RetailerPrices
                    .OrderBy(r => r.PriceEur)
                    .Take(3)
                    .Select(r => new RetailerPriceDto(r.Retailer, r.Url, r.PriceEur, r.InStock))
                    .ToList());

        return new LureDetailDto(
            Id: lure.Id,
            Slug: lure.Slug,
            Name: name,
            Brand: brand,
            LureType: lure.LureType,
            WaterType: lure.WaterType,
            WeightG: weightG,
            PrimaryImageUrl: primaryImage?.Url,
            PrimaryColorHex: firstColor?.HexCodes.FirstOrDefault()?.Hex,
            TargetSpecies: targetSpecies.Select(ts => ts.Species.Slug).ToArray(),
            PriceAvgEur: lure.Price6mAvgEur,
            FavoritesCount: favoritesCount,
            IsFavorited: null,
            Description: description,
            LengthMm: lengthMm,
            DepthMinM: lure.DepthMinM,
            DepthMaxM: lure.DepthMaxM,
            HookSize: lure.HookSize,
            HookType: lure.HookType,
            HookCount: lure.HookCount,
            Material: lure.Material,
            Sizes: orderedSizes.Select(s =>
                new LureSizeDto(s.Id, s.Code, s.Label, s.LengthMm, s.WeightG)).ToList(),
            Colors: lure.Colors.Select(c =>
                new LureColorDto(
                    c.Id, c.NamePt,
                    c.HexCodes.ElementAtOrDefault(0)?.Hex,
                    c.HexCodes.ElementAtOrDefault(1)?.Hex,
                    c.Pattern,
                    c.HexCodes.Select(h => new LureHexCodeDto(h.Hex, h.Label)).ToList())).ToList(),
            Images: lure.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
                .Select(i => new LureImageDto(i.Url, i.ColorId, i.IsPrimary)).ToList(),
            TargetSpeciesDetail: targetSpecies.Select(ts => new TargetSpeciesDto(
                ts.Species.Slug,
                Tr(ts.Species.Translations, locale) ?? ts.Species.Slug,
                ts.Confidence)).ToList(),
            Pricing: pricing,
            AvgRating: reviews.Count > 0 ? reviews.Average(r => (double)r) : null,
            ReviewsCount: reviews.Count,
            IsInInventory: null);
    }

    private static string? Tr(IEnumerable<LureTranslation> translations, string locale)
        => translations.FirstOrDefault(t => t.Locale == locale)?.Name
           ?? translations.FirstOrDefault(t => t.Locale == "pt")?.Name;

    private static string? Tr(IEnumerable<SpeciesTranslation> translations, string locale)
        => translations.FirstOrDefault(t => t.Locale == locale)?.CommonName
           ?? translations.FirstOrDefault(t => t.Locale == "pt")?.CommonName;
}
