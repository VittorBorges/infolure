using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infolure.Api.Infrastructure.Persistence.Auditing;

/// <summary>
/// Feature 002 (T006): centraliza a mecânica auditável.
/// - Converte qualquer remoção (EntityState.Deleted) de uma entidade <see cref="IAuditable"/>
///   num soft-delete (DeletedAt = agora), preservando o registo (reversível).
/// - Carimba UpdatedAt nas entidades que o expõem (Lure) ao criar/alterar.
/// O global query filter (DeletedAt == null) garante que o soft-delete some das consultas.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Apply(DbContext? context)
    {
        if (context is null) return;
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Deleted when entry.Entity is ISoftDeletable:
                    // Soft-delete reversível: não apaga, marca como eliminado.
                    // Apenas para conteúdo com restauro; toggles/ligações mantêm hard-delete.
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = now;
                    StampUpdated(entry.Entity, now);
                    break;
                case EntityState.Modified:
                    StampUpdated(entry.Entity, now);
                    break;
            }
        }
    }

    // UpdatedAt só existe em entidades específicas (Lure); carimbar quando presente.
    private static void StampUpdated(IAuditable entity, DateTimeOffset now)
    {
        if (entity is Lure lure) lure.UpdatedAt = now;
    }
}
