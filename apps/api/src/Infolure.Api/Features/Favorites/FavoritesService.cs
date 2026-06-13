using Infolure.Api.Features.Auth;
using Infolure.Api.Features.Catalog;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Infolure.Api.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Features.Favorites;

public enum FavoriteResult { Ok, UserNotFound, LureNotFound }

/// <summary>
/// Favoritos (US-05). Toggle persiste em user_lure_favorites e recalcula a popularidade
/// da isca no Typesense (write-through — T057). A listagem devolve os favoritos como cards.
/// </summary>
public class FavoritesService(AppDbContext db, UserResolver users, LureIndexer indexer, ILogger<FavoritesService> logger)
{
    public async Task<FavoriteResult> AddAsync(string sub, Guid lureId, CancellationToken ct = default)
    {
        var userId = await users.ResolveUserIdAsync(sub, ct);
        if (userId is null) return FavoriteResult.UserNotFound;
        if (!await db.Lures.AnyAsync(l => l.Id == lureId, ct)) return FavoriteResult.LureNotFound;

        var exists = await db.UserLureFavorites.AnyAsync(f => f.UserId == userId && f.LureId == lureId, ct);
        if (!exists)
        {
            db.UserLureFavorites.Add(new UserLureFavorite { UserId = userId.Value, LureId = lureId });
            await db.SaveChangesAsync(ct);
            await SafeReindex(lureId, ct);
        }
        return FavoriteResult.Ok;
    }

    public async Task<FavoriteResult> RemoveAsync(string sub, Guid lureId, CancellationToken ct = default)
    {
        var userId = await users.ResolveUserIdAsync(sub, ct);
        if (userId is null) return FavoriteResult.UserNotFound;

        var fav = await db.UserLureFavorites.FirstOrDefaultAsync(f => f.UserId == userId && f.LureId == lureId, ct);
        if (fav is not null)
        {
            db.UserLureFavorites.Remove(fav);
            await db.SaveChangesAsync(ct);
            await SafeReindex(lureId, ct);
        }
        return FavoriteResult.Ok;
    }

    public async Task<LureListResponse?> ListAsync(string sub, int page, int perPage, string locale, CancellationToken ct = default)
    {
        var userId = await users.ResolveUserIdAsync(sub, ct);
        if (userId is null) return null;

        page = Math.Max(1, page);
        perPage = Math.Clamp(perPage, 1, 50);

        var favoritedQuery = db.Lures
            .Where(l => db.UserLureFavorites.Any(f => f.UserId == userId && f.LureId == l.Id));

        var total = await favoritedQuery.CountAsync(ct);
        var lures = await favoritedQuery
            .OrderByDescending(l => db.UserLureFavorites
                .Where(f => f.UserId == userId && f.LureId == l.Id)
                .Select(f => f.CreatedAt)
                .FirstOrDefault())
            .Skip((page - 1) * perPage).Take(perPage)
            .Include(l => l.Brand).ThenInclude(b => b!.Translations)
            .Include(l => l.Translations)
            .Include(l => l.Images)
            .Include(l => l.TargetSpecies).ThenInclude(ts => ts.Species)
            .AsNoTracking()
            .ToListAsync(ct);

        var favCounts = await db.UserLureFavorites
            .Where(f => lures.Select(l => l.Id).Contains(f.LureId))
            .GroupBy(f => f.LureId).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var data = lures.Select(l => new LureCardDto(
            Id: l.Id,
            Slug: l.Slug,
            Name: l.Translations.FirstOrDefault(t => t.Locale == locale)?.Name
                  ?? l.Translations.FirstOrDefault(t => t.Locale == "pt")?.Name ?? l.Slug,
            Brand: l.Brand?.Translations.FirstOrDefault(t => t.Locale == "pt")?.Name,
            LureType: l.LureType,
            WaterType: l.WaterType,
            WeightG: l.WeightG,
            PrimaryImageUrl: l.Images.FirstOrDefault(i => i.IsPrimary)?.Url ?? l.Images.FirstOrDefault()?.Url,
            PrimaryColorHex: null,
            TargetSpecies: l.TargetSpecies.Select(ts => ts.Species.Slug).ToArray(),
            PriceAvgEur: l.Price6mAvgEur,
            FavoritesCount: favCounts.GetValueOrDefault(l.Id, 0),
            IsFavorited: true)).ToList();

        var meta = new ListMeta(total, page, perPage, new CatalogFacets([], [], [], []));
        return new LureListResponse(data, meta);
    }

    private async Task SafeReindex(Guid lureId, CancellationToken ct)
    {
        try { await indexer.ReindexLureAsync(lureId, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Reindex de popularidade falhou para {LureId}", lureId); }
    }
}
