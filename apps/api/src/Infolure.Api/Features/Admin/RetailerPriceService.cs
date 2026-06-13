using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Infolure.Api.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Features.Admin;

/// <summary>
/// Gestão de preços de retalhistas (T080). Ao adicionar um preço, recalcula
/// price_6m_min/max/avg da isca e reindexa no Typesense (o preço afeta ordenação/facets).
/// </summary>
public class RetailerPriceService(AppDbContext db, LureIndexer indexer)
{
    public async Task<PriceSummary?> AddPriceAsync(Guid lureId, AddRetailerPriceRequest req, CancellationToken ct = default)
    {
        var lure = await db.Lures.FirstOrDefaultAsync(l => l.Id == lureId, ct);
        if (lure is null) return null;

        db.LureRetailerPrices.Add(new LureRetailerPrice
        {
            Id = Guid.NewGuid(),
            LureId = lureId,
            Retailer = req.Retailer,
            Url = req.Url,
            PriceEur = req.PriceEur,
            InStock = req.InStock,
        });
        await db.SaveChangesAsync(ct);

        var prices = await db.LureRetailerPrices
            .Where(p => p.LureId == lureId)
            .Select(p => p.PriceEur)
            .ToListAsync(ct);

        lure.Price6mMinEur = prices.Min();
        lure.Price6mMaxEur = prices.Max();
        lure.Price6mAvgEur = Math.Round(prices.Average(), 2);
        lure.Price6mUpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        try { await indexer.ReindexLureAsync(lureId, ct); } catch { /* índice best-effort */ }

        return new PriceSummary(lure.Price6mMinEur, lure.Price6mMaxEur, lure.Price6mAvgEur);
    }
}
