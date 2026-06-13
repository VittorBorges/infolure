using Infolure.Api.Features.Auth;
using Infolure.Api.Features.Catalog;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Features.Inventory;

/// <summary>
/// Inventário pessoal ("iscas que possuo" — US-06). Valida quantidade (1–99), condição e notas
/// (≤200). Unicidade por (utilizador, isca, cor). Apenas o dono pode editar/remover.
/// </summary>
public class InventoryService(AppDbContext db, UserResolver users)
{
    private static readonly string[] AddConditions = ["new", "good", "used"];
    private static readonly string[] UpdateConditions = ["new", "good", "used", "lost"];

    public async Task<(InventoryResult Result, InventoryEntryDto? Entry)> AddAsync(
        string sub, AddInventoryRequest req, CancellationToken ct = default)
    {
        var userId = await users.ResolveUserIdAsync(sub, ct);
        if (userId is null) return (InventoryResult.UserNotFound, null);

        if (req.Quantity is < 1 or > 99) return (InventoryResult.Invalid, null);
        if (req.Condition is not null && !AddConditions.Contains(req.Condition)) return (InventoryResult.Invalid, null);
        if (req.Notes is { Length: > 200 }) return (InventoryResult.Invalid, null);
        if (!await db.Lures.AnyAsync(l => l.Id == req.LureId, ct)) return (InventoryResult.LureNotFound, null);

        var dup = await db.UserLureInventory.AnyAsync(
            i => i.UserId == userId && i.LureId == req.LureId && i.ColorId == req.ColorId, ct);
        if (dup) return (InventoryResult.Conflict, null);

        var entry = new UserLureInventory
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            LureId = req.LureId,
            ColorId = req.ColorId,
            Quantity = (short)req.Quantity,
            Condition = req.Condition,
            Notes = req.Notes,
        };
        db.UserLureInventory.Add(entry);
        await db.SaveChangesAsync(ct);
        return (InventoryResult.Ok, await MapAsync(entry.Id, ct));
    }

    public async Task<(InventoryResult Result, InventoryEntryDto? Entry)> UpdateAsync(
        string sub, Guid entryId, UpdateInventoryRequest req, CancellationToken ct = default)
    {
        var userId = await users.ResolveUserIdAsync(sub, ct);
        if (userId is null) return (InventoryResult.UserNotFound, null);

        var entry = await db.UserLureInventory.FirstOrDefaultAsync(i => i.Id == entryId, ct);
        if (entry is null) return (InventoryResult.NotFound, null);
        if (entry.UserId != userId) return (InventoryResult.NotOwner, null);

        if (req.Quantity is { } q && q is < 1 or > 99) return (InventoryResult.Invalid, null);
        if (req.Condition is not null && !UpdateConditions.Contains(req.Condition)) return (InventoryResult.Invalid, null);
        if (req.Notes is { Length: > 200 }) return (InventoryResult.Invalid, null);

        if (req.Quantity is { } qty) entry.Quantity = (short)qty;
        if (req.Condition is not null) entry.Condition = req.Condition;
        if (req.Notes is not null) entry.Notes = req.Notes;
        await db.SaveChangesAsync(ct);
        return (InventoryResult.Ok, await MapAsync(entry.Id, ct));
    }

    public async Task<InventoryResult> DeleteAsync(string sub, Guid entryId, CancellationToken ct = default)
    {
        var userId = await users.ResolveUserIdAsync(sub, ct);
        if (userId is null) return InventoryResult.UserNotFound;

        var entry = await db.UserLureInventory.FirstOrDefaultAsync(i => i.Id == entryId, ct);
        if (entry is null) return InventoryResult.NotFound;
        if (entry.UserId != userId) return InventoryResult.NotOwner;

        db.UserLureInventory.Remove(entry);
        await db.SaveChangesAsync(ct);
        return InventoryResult.Ok;
    }

    public async Task<InventoryListResponse?> ListAsync(string sub, string locale, CancellationToken ct = default)
    {
        var userId = await users.ResolveUserIdAsync(sub, ct);
        if (userId is null) return null;

        var entries = await db.UserLureInventory
            .Where(i => i.UserId == userId)
            .OrderBy(i => i.Lure.LureType)
            .Include(i => i.Lure).ThenInclude(l => l.Brand).ThenInclude(b => b!.Translations)
            .Include(i => i.Lure).ThenInclude(l => l.Translations)
            .Include(i => i.Lure).ThenInclude(l => l.Images)
            .Include(i => i.Lure).ThenInclude(l => l.TargetSpecies).ThenInclude(ts => ts.Species)
            .AsNoTracking()
            .ToListAsync(ct);

        var lureIds = entries.Select(e => e.LureId).Distinct().ToList();
        var favCounts = await db.UserLureFavorites
            .Where(f => lureIds.Contains(f.LureId))
            .GroupBy(f => f.LureId).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var colorIds = entries.Where(e => e.ColorId != null).Select(e => e.ColorId!.Value).Distinct().ToList();
        var colors = await db.LureColors.Where(c => colorIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c, ct);

        var data = entries.Select(e => ToEntryDto(e, e.Lure, favCounts.GetValueOrDefault(e.LureId, 0),
            e.ColorId is { } cid ? colors.GetValueOrDefault(cid) : null, locale)).ToList();

        return new InventoryListResponse(data, lureIds.Count);
    }

    private async Task<InventoryEntryDto> MapAsync(Guid entryId, CancellationToken ct)
    {
        var e = await db.UserLureInventory
            .Where(i => i.Id == entryId)
            .Include(i => i.Lure).ThenInclude(l => l.Brand).ThenInclude(b => b!.Translations)
            .Include(i => i.Lure).ThenInclude(l => l.Translations)
            .Include(i => i.Lure).ThenInclude(l => l.Images)
            .Include(i => i.Lure).ThenInclude(l => l.TargetSpecies).ThenInclude(ts => ts.Species)
            .AsNoTracking()
            .FirstAsync(ct);
        var fav = await db.UserLureFavorites.CountAsync(f => f.LureId == e.LureId, ct);
        var color = e.ColorId is { } cid ? await db.LureColors.FindAsync([cid], ct) : null;
        return ToEntryDto(e, e.Lure, fav, color, "pt");
    }

    private static InventoryEntryDto ToEntryDto(UserLureInventory e, Lure l, int favCount, LureColor? color, string locale)
    {
        var card = new LureCardDto(
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
            FavoritesCount: favCount,
            IsFavorited: null);

        var colorDto = color is null ? null : new InventoryColorDto(color.Id, color.NamePt, color.HexPrimary);
        return new InventoryEntryDto(e.Id, card, colorDto, e.Quantity, e.Condition, e.Notes, e.AddedAt);
    }
}
