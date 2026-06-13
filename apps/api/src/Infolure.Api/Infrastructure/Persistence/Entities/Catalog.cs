namespace Infolure.Api.Infrastructure.Persistence.Entities;

// Domínio de catálogo — espelha data-model.md (dialeto PostgreSQL, naming snake_case).

public class Brand
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = null!;
    public ICollection<BrandTranslation> Translations { get; set; } = new List<BrandTranslation>();
    public ICollection<Lure> Lures { get; set; } = new List<Lure>();
}

public class BrandTranslation
{
    public Guid BrandId { get; set; }
    public string Locale { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Brand Brand { get; set; } = null!;
}

public class Species
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = null!;
    public string? Family { get; set; }
    public string? WaterType { get; set; } // freshwater | saltwater | both
    public ICollection<SpeciesTranslation> Translations { get; set; } = new List<SpeciesTranslation>();
}

public class SpeciesTranslation
{
    public Guid SpeciesId { get; set; }
    public string Locale { get; set; } = null!;
    public string CommonName { get; set; } = null!;
    public Species Species { get; set; } = null!;
}

public class Lure
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = null!;
    public Guid? BrandId { get; set; }
    public string? ModelRef { get; set; } // referência de modelo do fabricante (busca US-02)
    public string LureType { get; set; } = null!;
    public string? WaterType { get; set; }
    public decimal? WeightG { get; set; }
    public decimal? LengthMm { get; set; }
    public decimal? DepthMinM { get; set; }
    public decimal? DepthMaxM { get; set; }
    public string? HookSize { get; set; }
    public string? HookType { get; set; }
    public short? HookCount { get; set; }
    public string? Material { get; set; }
    public string Attributes { get; set; } = "{}"; // JSONB
    public decimal? Price6mMinEur { get; set; }
    public decimal? Price6mMaxEur { get; set; }
    public decimal? Price6mAvgEur { get; set; }
    public DateTimeOffset? Price6mUpdatedAt { get; set; }
    public string Status { get; set; } = "draft"; // draft | published | archived
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Brand? Brand { get; set; }
    public ICollection<LureTranslation> Translations { get; set; } = new List<LureTranslation>();
    public ICollection<LureColor> Colors { get; set; } = new List<LureColor>();
    public ICollection<LureImage> Images { get; set; } = new List<LureImage>();
    public ICollection<LureTargetSpecies> TargetSpecies { get; set; } = new List<LureTargetSpecies>();
    public ICollection<LureRetailerPrice> RetailerPrices { get; set; } = new List<LureRetailerPrice>();
}

public class LureTranslation
{
    public Guid LureId { get; set; }
    public string Locale { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Lure Lure { get; set; } = null!;
}

public class LureColor
{
    public Guid Id { get; set; }
    public Guid LureId { get; set; }
    public string NamePt { get; set; } = null!;
    public string? NameEn { get; set; }
    public string? HexPrimary { get; set; }
    public string? HexSecondary { get; set; }
    public string? Pattern { get; set; }
    public Lure Lure { get; set; } = null!;
}

public class LureImage
{
    public Guid Id { get; set; }
    public Guid LureId { get; set; }
    public Guid? ColorId { get; set; }
    public string Url { get; set; } = null!;
    public short SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public Lure Lure { get; set; } = null!;
}

public class LureTargetSpecies
{
    public Guid LureId { get; set; }
    public Guid SpeciesId { get; set; }
    public string? Confidence { get; set; } // primary | secondary
    public Lure Lure { get; set; } = null!;
    public Species Species { get; set; } = null!;
}

public class LureRetailerPrice
{
    public Guid Id { get; set; }
    public Guid LureId { get; set; }
    public string Retailer { get; set; } = null!;
    public string? Url { get; set; }
    public decimal PriceEur { get; set; }
    public bool InStock { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; }
    public Lure Lure { get; set; } = null!;
}
