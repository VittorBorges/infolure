namespace Infolure.Api.Infrastructure.Persistence.Auditing;

/// <summary>
/// Base auditável transversal (Feature 002, Pilar 0). Os três atributos de estado são
/// ortogonais entre si e independentes de qualquer estado editorial (ex.: Lure.Status).
/// Aplicada por convenção a todas as entidades de domínio: o global query filter
/// (DeletedAt == null) e o <see cref="AuditSaveChangesInterceptor"/> tratam-nas uniformemente.
/// </summary>
public interface IAuditable
{
    /// <summary>Ativo/inativo. Inativo é ocultado das superfícies públicas.</summary>
    bool IsActive { get; set; }

    /// <summary>Origem do registo: manual | automation | import.</summary>
    string Source { get; set; }

    /// <summary>Soft-delete reversível. null = vivo. Não confundir com eliminação RGPD efetiva.</summary>
    DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>
/// Subconjunto de <see cref="IAuditable"/> cujo Remove é convertido em soft-delete reversível
/// pelo <see cref="AuditSaveChangesInterceptor"/> (entidades de conteúdo com semântica de restauro).
/// As restantes IAuditable mantêm hard-delete (toggles/ligações: favoritos, votos, inventário),
/// embora continuem a expor os 3 campos de estado. Conjunto alargado nas user stories seguintes.
/// </summary>
public interface ISoftDeletable : IAuditable;

/// <summary>Valores canónicos para <see cref="IAuditable.Source"/>.</summary>
public static class AuditSource
{
    public const string Manual = "manual";
    public const string Automation = "automation";
    public const string Import = "import";
}
