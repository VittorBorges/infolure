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
    private static IQueryable<Persistence.Entities.Lure> WithGraph(IQueryable<Persistence.Entities.Lure> q)
        => q.Include(l => l.Brand).ThenInclude(b => b!.Translations)
            .Include(l => l.Translations)
            .Include(l => l.Images)
            .Include(l => l.TargetSpecies).ThenInclude(ts => ts.Species)
            .AsNoTracking();

    // Elegibilidade pública para indexação (F002 US-01): publicada + ativa + marca-pai ativa.
    // DeletedAt é tratado pelo global query filter (linhas soft-deleted não aparecem em db.Lures).
    private IQueryable<Persistence.Entities.Lure> EligibleForIndex(IQueryable<Persistence.Entities.Lure> q)
        => q.Where(l => l.Status == "published" && l.IsActive)
            .Where(l => l.BrandId == null || db.Brands.Any(b => b.Id == l.BrandId && b.IsActive));

    public async Task<int> ReindexAllAsync(CancellationToken ct = default)
    {
        var lures = await WithGraph(EligibleForIndex(db.Lures)).ToListAsync(ct);
        if (lures.Count == 0) return 0;

        var docs = new List<LureSearchDocument>();
        foreach (var l in lures)
            docs.Add(await BuildDocAsync(l, ct));

        await typesense.ImportDocuments(TypesenseExtensions.CollectionName, docs, docs.Count, ImportType.Upsert);
        return docs.Count;
    }

    /// <summary>
    /// Reindexa uma única isca write-through. Se a isca deixou de ser elegível (despublicada,
    /// inativa, eliminada ou marca-pai inativa), é REMOVIDA do índice (F002 US-01 / FR-011).
    /// </summary>
    public async Task ReindexLureAsync(Guid lureId, CancellationToken ct = default)
    {
        var lure = await WithGraph(EligibleForIndex(db.Lures.Where(l => l.Id == lureId)))
            .FirstOrDefaultAsync(ct);

        if (lure is null)
        {
            await RemoveLureAsync(lureId, ct);
            return;
        }

        var doc = await BuildDocAsync(lure, ct);
        await typesense.ImportDocuments(TypesenseExtensions.CollectionName, [doc], 1, ImportType.Upsert);
    }

    /// <summary>Remove uma isca do índice de busca (best-effort; ignora ausência).</summary>
    public async Task RemoveLureAsync(Guid lureId, CancellationToken ct = default)
    {
        try
        {
            await typesense.DeleteDocument<LureSearchDocument>(TypesenseExtensions.CollectionName, lureId.ToString());
        }
        catch
        {
            // documento inexistente no índice — nada a remover
        }
    }

    private async Task<LureSearchDocument> BuildDocAsync(Persistence.Entities.Lure l, CancellationToken ct)
    {
        var fav = await db.UserLureFavorites.CountAsync(f => f.LureId == l.Id, ct);
        var inv = await db.UserLureInventory.CountAsync(i => i.LureId == l.Id, ct);
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
    }

    private static string NameFor(Persistence.Entities.Lure l, string locale)
        => l.Translations.FirstOrDefault(t => t.Locale == locale)?.Name
           ?? l.Translations.FirstOrDefault(t => t.Locale == "pt")?.Name
           ?? l.Slug;

    private static string? NameForOrNull(Persistence.Entities.Lure l, string locale)
        => l.Translations.FirstOrDefault(t => t.Locale == locale)?.Name;
}
