using Infolure.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Features.Admin;

/// <summary>T028 (FR-008): métricas de cadastros e estados para o dashboard do painel.</summary>
public class DashboardService(AppDbContext db)
{
    public async Task<object> GetAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var d7 = now.AddDays(-7);
        var d30 = now.AddDays(-30);

        var usersTotal = await db.Users.CountAsync(ct);
        var new7 = await db.Users.CountAsync(u => u.CreatedAt >= d7, ct);
        var new30 = await db.Users.CountAsync(u => u.CreatedAt >= d30, ct);

        var series = await db.Users
            .Where(u => u.CreatedAt >= d30)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { date = g.Key, count = g.Count() })
            .OrderBy(x => x.date)
            .ToListAsync(ct);

        var byStatus = await db.Lures.GroupBy(l => l.Status)
            .Select(g => new { g.Key, c = g.Count() }).ToListAsync(ct);
        var bySource = await db.Lures.GroupBy(l => l.Source)
            .Select(g => new { g.Key, c = g.Count() }).ToListAsync(ct);
        var luresActive = await db.Lures.CountAsync(l => l.IsActive, ct);
        var luresInactive = await db.Lures.CountAsync(l => !l.IsActive, ct);

        var reviewsPending = await db.LureReviews.CountAsync(r => r.Status == "pending", ct);
        var favoritesTotal = await db.UserLureFavorites.CountAsync(ct);
        var inventoryTotal = await db.UserLureInventory.CountAsync(ct);

        return new
        {
            users = new
            {
                total = usersTotal,
                new_7d = new7,
                new_30d = new30,
                series = series.Select(s => new { date = s.date.ToString("yyyy-MM-dd"), count = s.count }),
            },
            lures = new
            {
                by_status = byStatus.ToDictionary(x => x.Key, x => x.c),
                by_source = bySource.ToDictionary(x => x.Key, x => x.c),
                active = luresActive,
                inactive = luresInactive,
            },
            reviews = new { pending = reviewsPending },
            favorites = new { total = favoritesTotal },
            inventory = new { total = inventoryTotal },
        };
    }
}
