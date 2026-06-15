using System.Text.Json;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infolure.Api.Infrastructure.Persistence.Auditing;

/// <summary>
/// T026 (FR-020/FR-020a/SC-007): quando o <see cref="IAdminActionContext"/> está ativo, emite uma
/// entrada em <c>admin_audit_log</c> por entidade de domínio escrita. As entradas são adicionadas
/// na mesma transação do SaveChanges. Para dados pessoais (contas, favoritos, inventário) inclui um
/// instantâneo dos campos alterados (antes→depois). Corre DEPOIS do AuditSaveChangesInterceptor,
/// para já observar o soft-delete (DeletedAt) aplicado.
/// </summary>
public sealed class AdminAuditInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<Type> PersonalDataTypes =
        [typeof(User), typeof(UserLureFavorite), typeof(UserLureInventory)];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private static void Capture(DbContext? context)
    {
        if (context is not AppDbContext db || db.AdminContext?.IsActive != true) return;

        var now = DateTimeOffset.UtcNow;
        var actor = db.AdminContext.ActorUserId;

        // Materializar antes de adicionar (não mutar o ChangeTracker durante a enumeração).
        var targets = db.ChangeTracker.Entries<IAuditable>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var entries = new List<AdminAuditEntry>();
        foreach (var e in targets)
        {
            var personal = PersonalDataTypes.Contains(e.Metadata.ClrType);
            entries.Add(new AdminAuditEntry
            {
                Id = Guid.NewGuid(),
                ActorUserId = actor,
                Action = Classify(e),
                EntityType = e.Metadata.GetTableName() ?? e.Metadata.ClrType.Name,
                EntityId = PrimaryKey(e),
                IsPersonalData = personal,
                Changes = personal ? Snapshot(e) : null,
                CreatedAt = now,
            });
        }

        if (entries.Count > 0) db.AdminAuditLog.AddRange(entries);
    }

    private static string Classify(EntityEntry e)
    {
        if (e.State == EntityState.Added) return "create";
        if (e.State == EntityState.Deleted) return "delete"; // hard-delete (entidade não soft-deletable)

        // Modified: distinguir soft-delete / restore / (de)ativação de um update normal.
        if (Changed(e, nameof(IAuditable.DeletedAt), out var delBefore, out var delAfter))
            return delBefore is null && delAfter is not null ? "delete"
                 : delBefore is not null && delAfter is null ? "restore" : "update";

        if (Changed(e, nameof(IAuditable.IsActive), out var actBefore, out _))
            return actBefore is true ? "deactivate" : "activate";

        return "update";
    }

    private static bool Changed(EntityEntry e, string prop, out object? before, out object? after)
    {
        before = after = null;
        var p = e.Properties.FirstOrDefault(x => x.Metadata.Name == prop);
        if (p is null || e.State != EntityState.Modified || !p.IsModified) return false;
        before = p.OriginalValue;
        after = p.CurrentValue;
        return !Equals(before, after);
    }

    private static string PrimaryKey(EntityEntry e)
    {
        var key = e.Metadata.FindPrimaryKey();
        if (key is null) return "";
        var parts = key.Properties.Select(p => e.Property(p.Name).CurrentValue?.ToString() ?? "");
        return string.Join(":", parts);
    }

    private static string? Snapshot(EntityEntry e)
    {
        if (e.State != EntityState.Modified) return null;
        var diff = new Dictionary<string, object?>();
        foreach (var p in e.Properties.Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)))
            diff[p.Metadata.Name] = new { before = p.OriginalValue, after = p.CurrentValue };
        return diff.Count == 0 ? null : JsonSerializer.Serialize(diff);
    }
}
