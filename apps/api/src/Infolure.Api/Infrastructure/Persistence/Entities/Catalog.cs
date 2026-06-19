using Infolure.Api.Infrastructure.Persistence.Auditing;

namespace Infolure.Api.Infrastructure.Persistence.Entities;

// Domínio de catálogo — espelha data-model.md (dialeto PostgreSQL, naming snake_case).
// Feature 002: todas as entidades implementam IAuditable (IsActive/Source/DeletedAt).

public class Brand : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = null!;
    public ICollection<BrandTranslation> Translations { get; set; } = new List<BrandTranslation>();
    public ICollection<Lure> Lures { get; set; } = new List<Lure>();

    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = AuditSource.Manual;
    public DateTimeOffset? DeletedAt { get; set; }
}

public class BrandTranslation : IAuditable
{
    public Guid BrandId { get; set; }
    public string Locale { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Brand Brand { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = AuditSource.Manual;
    public DateTimeOffset? DeletedAt { get; set; }
}

public class Species : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = null!;
    public string? Family { get; set; }
    public string? WaterType { get; set; } // freshwater | saltwater | both
    public ICollection<SpeciesTranslation> Translations { get; set; } = new List<SpeciesTranslation>();

    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = AuditSource.Manual;
    public DateTimeOffset? DeletedAt { get; set; }
}

public class SpeciesTranslation : IAuditable
{
    public Guid SpeciesId { get; set; }
    public string Locale { get; set; } = null!;
    public string CommonName { get; set; } = null!;
    public Species Species { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = AuditSource.Manual;
    public DateTimeOffset? DeletedAt { get; set; }
}

public class Lure : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = null!;
    public Guid? BrandId { get; set; }
    public string? ModelRef { get; set; } // referência de modelo do fabricante (busca US-02)
    public string LureType { get; set; } = null!;
    public string? WaterType { get; set; }
    public decimal? DepthMinM { get; set; }
    public decimal? DepthMaxM { get; set; }
    public string? Material { get; set; }
    public string Attributes { get; set; } = "{}"; // JSONB
    public decimal? Price6mMinEur { get; set; }
    public decimal? Price6mMaxEur { get; set; }
    public decimal? Price6mAvgEur { get; set; }
    public DateTimeOffset? Price6mUpdatedAt { get; set; }
    public string Status { get; set; } = "draft"; // draft | published | archived (editorial)
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    // Feature 006: anzol movido para LureConfiguration; indexação SEO passou a flag global (sem IsIndexable por isca).

    public Brand? Brand { get; set; }
    public ICollection<LureTranslation> Translations { get; set; } = new List<LureTranslation>();
    public ICollection<LureConfiguration> Configurations { get; set; } = new List<LureConfiguration>();  // Feature 006 — antes "Sizes" (fonte única de peso/comprimento + anzol)
    public ICollection<LureColor> Colors { get; set; } = new List<LureColor>();
    public ICollection<LureImage> Images { get; set; } = new List<LureImage>();
    public ICollection<LureTargetSpecies> TargetSpecies { get; set; } = new List<LureTargetSpecies>();
    public ICollection<LureRetailerPrice> RetailerPrices { get; set; } = new List<LureRetailerPrice>();

    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = AuditSource.Manual;
    public DateTimeOffset? DeletedAt { get; set; }
}

public class LureTranslation : IAuditable
{
    public Guid LureId { get; set; }
    public string Locale { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Lure Lure { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = AuditSource.Manual;
    public DateTimeOffset? DeletedAt { get; set; }
}

// Feature 006 — "Configuração da isca" (antes "LureSize"/tamanho). Fonte única de peso/comprimento
// e, agora, dos dados de anzol (cada configuração tem o seu anzol). Tabela lure_configurations.
public class LureConfiguration : IAuditable
{
    public Guid Id { get; set; }
    public Guid LureId { get; set; }
    public string? Code { get; set; }          // código curto/SKU opcional
    public string Label { get; set; } = null!;  // designação do fabricante, ex.: "100SP"
    public decimal? LengthMm { get; set; }      // comprimento (mm)
    public decimal WeightG { get; set; }        // peso (g) — obrigatório por configuração
    public string? HookSize { get; set; }       // Feature 006 — anzol por configuração
    public string? HookType { get; set; }
    public short? HookCount { get; set; }
    public short SortOrder { get; set; }
    public Lure Lure { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = AuditSource.Manual;
    public DateTimeOffset? DeletedAt { get; set; }
}

// Feature 005 — sub-objeto JSON de uma cor: código HTML (hex) + cor de base opcional (label).
// Persistido na coluna jsonb lure_colors.hex_codes (owned-collection .ToJson()). Duplicados
// dentro da mesma cor são permitidos (podem ter textura diferente).
public class LureHexCode
{
    public string Hex { get; set; } = null!;   // "#RGB" ou "#RRGGBB" (minúsculas)
    public string? Label { get; set; }          // cor de base, ex.: "verde"
}

public class LureColor : IAuditable
{
    public Guid Id { get; set; }
    public Guid LureId { get; set; }
    public string NamePt { get; set; } = null!;
    public string? NameEn { get; set; }
    public List<LureHexCode> HexCodes { get; set; } = new();  // jsonb — lista aberta de hex
    public string? Pattern { get; set; }
    public Lure Lure { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = AuditSource.Manual;
    public DateTimeOffset? DeletedAt { get; set; }
}

public class LureImage : IAuditable
{
    public Guid Id { get; set; }
    public Guid LureId { get; set; }
    public Guid? ColorId { get; set; }
    public string Url { get; set; } = null!;
    public short SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public Lure Lure { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = AuditSource.Manual;
    public DateTimeOffset? DeletedAt { get; set; }
}

public class LureTargetSpecies : IAuditable
{
    public Guid LureId { get; set; }
    public Guid SpeciesId { get; set; }
    public string? Confidence { get; set; } // primary | secondary
    public Lure Lure { get; set; } = null!;
    public Species Species { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = AuditSource.Manual;
    public DateTimeOffset? DeletedAt { get; set; }
}

public class LureRetailerPrice : IAuditable
{
    public Guid Id { get; set; }
    public Guid LureId { get; set; }
    public string Retailer { get; set; } = null!;
    public string? Url { get; set; }
    public decimal PriceEur { get; set; }
    public bool InStock { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; }
    public Lure Lure { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = AuditSource.Manual;
    public DateTimeOffset? DeletedAt { get; set; }
}
