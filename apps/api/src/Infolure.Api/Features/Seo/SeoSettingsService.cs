using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Infolure.Api.Features.Seo;

/// <summary>
/// T039 (US-03): interruptor global de indexação (app_settings, singleton) com cache Redis
/// (TTL ≤ 60s + invalidação na escrita, SC-005) e a lista de iscas elegíveis para o sitemap.
/// </summary>
public class SeoSettingsService(AppDbContext db, IServiceProvider sp)
{
    private const string CacheKey = "seo:indexing_enabled";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public record SitemapEntry(string Slug, DateTimeOffset UpdatedAt);

    private IDatabase? Cache => sp.GetService<IConnectionMultiplexer>()?.GetDatabase();

    public async Task<bool> GetEnabledAsync(CancellationToken ct = default)
    {
        var cache = Cache;
        if (cache is not null)
        {
            var cached = await cache.StringGetAsync(CacheKey);
            if (cached.HasValue) return cached == "1";
        }

        var enabled = await db.AppSettings.Where(s => s.Id == 1)
            .Select(s => (bool?)s.SeoIndexingEnabled).FirstOrDefaultAsync(ct) ?? true;

        if (cache is not null) await cache.StringSetAsync(CacheKey, enabled ? "1" : "0", Ttl);
        return enabled;
    }

    public async Task SetEnabledAsync(bool enabled, Guid? actor, CancellationToken ct = default)
    {
        var settings = await db.AppSettings.FirstOrDefaultAsync(s => s.Id == 1, ct);
        if (settings is null)
        {
            settings = new AppSetting { Id = 1 };
            db.AppSettings.Add(settings);
        }
        settings.SeoIndexingEnabled = enabled;
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        settings.UpdatedBy = actor;

        // app_settings não é IAuditable → auditar a alteração explicitamente (FR-020).
        db.AdminAuditLog.Add(new AdminAuditEntry
        {
            Id = Guid.NewGuid(), ActorUserId = actor, Action = "settings_update",
            EntityType = "app_settings", EntityId = "1", IsPersonalData = false,
            Changes = null, CreatedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(ct);

        var cache = Cache; // invalidação imediata (SC-005)
        if (cache is not null) await cache.KeyDeleteAsync(CacheKey);
    }

    /// <summary>Iscas elegíveis para sitemap: published+active+não-eliminado+indexable+marca ativa. Vazio se global off.</summary>
    public async Task<IReadOnlyList<SitemapEntry>> GetSitemapAsync(CancellationToken ct = default)
    {
        if (!await GetEnabledAsync(ct)) return [];

        return await db.Lures
            .Where(l => l.Status == "published" && l.IsActive && l.IsIndexable)
            .Where(l => l.BrandId == null || db.Brands.Any(b => b.Id == l.BrandId && b.IsActive))
            .OrderBy(l => l.Slug)
            .Select(l => new SitemapEntry(l.Slug, l.UpdatedAt))
            .ToListAsync(ct);
    }
}
