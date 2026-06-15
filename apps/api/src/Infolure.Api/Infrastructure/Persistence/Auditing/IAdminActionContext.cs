namespace Infolure.Api.Infrastructure.Persistence.Auditing;

/// <summary>
/// Contexto (scoped, por requisição) que sinaliza que a escrita atual é uma ação de administração.
/// Populado pelo <c>AdminAuditFilter</c> antes das ações do painel; lido pelo
/// <see cref="AdminAuditInterceptor"/> para emitir entradas de auditoria automaticamente
/// em TODAS as escritas admin (FR-020/SC-007), sem wiring por endpoint.
/// </summary>
public interface IAdminActionContext
{
    bool IsActive { get; }
    Guid? ActorUserId { get; }
    void Begin(Guid? actorUserId);
}

public sealed class AdminActionContext : IAdminActionContext
{
    public bool IsActive { get; private set; }
    public Guid? ActorUserId { get; private set; }

    public void Begin(Guid? actorUserId)
    {
        IsActive = true;
        ActorUserId = actorUserId;
    }
}
