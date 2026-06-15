using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Auditing;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Features.Admin;

/// <summary>
/// Base partilhada do CRUD admin (T027): list filtrável/paginada e mutações de ciclo de vida
/// (soft-delete/restore/toggle-active) genéricas sobre entidades IAuditable com chave Guid `Id`.
/// As mutações correm sob o contexto admin → auditadas automaticamente (AdminAuditInterceptor).
/// </summary>
public class AdminResourceService(AppDbContext db)
{
    public record ListQuery(string? Q, bool? IsActive, string? Source, string? Include, int Page, int PerPage);
    public record Paged(IReadOnlyList<object> Data, object Meta);

    public async Task<Paged> ListAsync<T>(IQueryable<T> baseQuery, ListQuery q, Func<T, object> project, CancellationToken ct)
        where T : class, IAuditable
    {
        var page = Math.Max(1, q.Page);
        var per = Math.Clamp(q.PerPage <= 0 ? 20 : q.PerPage, 1, 100);

        var query = q.Include switch
        {
            "all" => baseQuery.IgnoreQueryFilters(),
            "deleted" => baseQuery.IgnoreQueryFilters().Where(x => x.DeletedAt != null),
            "inactive" => baseQuery.Where(x => !x.IsActive),
            _ => baseQuery, // default: só vivos (global query filter)
        };
        if (q.IsActive is bool act) query = query.Where(x => x.IsActive == act);
        if (!string.IsNullOrWhiteSpace(q.Source)) query = query.Where(x => x.Source == q.Source);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * per).Take(per).ToListAsync(ct);
        return new Paged(items.Select(project).ToList(), new { total, page, per_page = per });
    }

    public Task<T?> FindAsync<T>(Guid id, CancellationToken ct) where T : class
        => db.Set<T>().IgnoreQueryFilters().FirstOrDefaultAsync(x => EF.Property<Guid>(x, "Id") == id, ct)!;

    public async Task<bool> DeleteAsync<T>(Guid id, CancellationToken ct) where T : class
    {
        var e = await FindAsync<T>(id, ct);
        if (e is null) return false;
        db.Set<T>().Remove(e); // interceptor → soft-delete se ISoftDeletable, senão hard-delete
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RestoreAsync<T>(Guid id, CancellationToken ct) where T : class, IAuditable
    {
        var e = await FindAsync<T>(id, ct);
        if (e is null) return false;
        e.DeletedAt = null;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetActiveAsync<T>(Guid id, bool active, CancellationToken ct) where T : class, IAuditable
    {
        var e = await FindAsync<T>(id, ct);
        if (e is null) return false;
        e.IsActive = active;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
