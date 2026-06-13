using Infolure.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Typesense;

namespace Infolure.Api.Infrastructure.Search;

/// <summary>
/// Sincronização write-through Postgres → Typesense (research.md §4). Mapeia iscas publicadas
/// para documentos de busca e faz upsert. Usado no bootstrap e (mais tarde) em mutações de catálogo.
/// </summary>
public class LureIndexer(AppDbContext db, ITypesenseClient typesense)
{
    public async Task<int> ReindexAllAsync(CancellationToken ct = default)
    {
        var lures = await db.Lures
            .Where(l => l.Status == "published")
            .Include(l => l.Brand).ThenInclude(b => b!.Translations)
            .Include(l => l.Translations)
            .Include(l => l.Images)
            .Include(l => l.TargetSpecies).ThenInclude(ts => ts.Species)
            .AsNoTracking()
            .ToListAsync(ct);

        if (lures.Count == 0) return 0;

        var favCounts = await db.UserLureFavorites
            .GroupBy(f => f.LureId).Select(g => new { LureId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LureId, x => x.Count, ct);
        var invCounts = await db.UserLureInventory
            .GroupBy(i => i.LureId).Select(g => new { LureId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LureId, x => x.Count, ct);

        var docs = lures.Select(l =>
        {
            var fav = favCounts.GetValueOrDefault(l.Id, 0);
            var inv = invCounts.GetValueOrDefault(l.Id, 0);
            return new LureSearchDocument
            {
                Id = l.Id.ToString(),
                Slug = l.Slug,
                ModelRef = l.ModelRef,
                NamePt = NameFor(l, "pt"),
                NameEn = NameForOrNull(l, "en"),
                NameEs = NameForOrNull(l, "es"),
                BrandName = l.Brand?.Translations.FirstOrDefault(t => t.Locale == "pt")?.Name ?? "",
                LureType = l.LureType,
                WaterType = l.WaterType ?? "",
                WeightG = (float?)l.WeightG,
                DepthMinM = (float?)l.DepthMinM,
                DepthMaxM = (float?)l.DepthMaxM,
                TargetSpecies = l.TargetSpecies.Select(ts => ts.Species.Slug).ToArray(),
                Price6mAvgEur = (float?)l.Price6mAvgEur,
                PrimaryImageUrl = l.Images.FirstOrDefault(i => i.IsPrimary)?.Url
                                  ?? l.Images.FirstOrDefault()?.Url,
                Status = l.Status,
                FavoritesCount = fav,
                PopularityScore = fav + inv,
                CreatedAt = l.CreatedAt.ToUnixTimeSeconds(),
            };
        }).ToList();

        await typesense.ImportDocuments(TypesenseExtensions.CollectionName, docs, docs.Count, ImportType.Upsert);
        return docs.Count;
    }

    private static string NameFor(Persistence.Entities.Lure l, string locale)
        => l.Translations.FirstOrDefault(t => t.Locale == locale)?.Name
           ?? l.Translations.FirstOrDefault(t => t.Locale == "pt")?.Name
           ?? l.Slug;

    private static string? NameForOrNull(Persistence.Entities.Lure l, string locale)
        => l.Translations.FirstOrDefault(t => t.Locale == locale)?.Name;
}
