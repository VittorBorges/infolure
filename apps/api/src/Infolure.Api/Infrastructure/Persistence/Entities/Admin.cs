namespace Infolure.Api.Infrastructure.Persistence.Entities;

// Feature 002 — entidades de administração/configuração. NÃO são IAuditable (não são domínio).

/// <summary>Configurações da aplicação (singleton, id = 1). Inclui o interruptor global de indexação.</summary>
public class AppSetting
{
    public short Id { get; set; } = 1;
    public bool SeoIndexingEnabled { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

/// <summary>Registo de auditoria das ações de escrita do painel (FR-020/FR-020a).</summary>
public class AdminAuditEntry
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = null!;     // create|update|activate|deactivate|delete|restore|moderate|settings_update
    public string EntityType { get; set; } = null!;  // ex.: 'lure', 'user'
    public string EntityId { get; set; } = null!;    // texto: suporta PKs compostas
    public bool IsPersonalData { get; set; }
    public string? Changes { get; set; }             // JSONB {before, after} — só para dados pessoais
    public DateTimeOffset CreatedAt { get; set; }
}
