using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Auditing;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Infrastructure.Seed;

/// <summary>
/// Seed de desenvolvimento (quickstart.md): 20 marcas, 20 espécies (com PT), 50 iscas.
/// Idempotente: só insere se a base estiver vazia.
/// </summary>
public static class Seeder
{
    private static readonly string[] LureTypes =
        ["jig", "crankbait", "softbait", "spinnerbait", "topwater", "jerkbait", "spoon", "swimbait"];
    private static readonly string[] WaterTypes = ["freshwater", "saltwater", "both"];

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Brands.AnyAsync(ct)) return;

        var brands = new List<Brand>();
        for (var i = 1; i <= 20; i++)
        {
            var brand = new Brand { Id = Guid.NewGuid(), Slug = $"marca-{i:00}" };
            brand.Translations.Add(new BrandTranslation
            {
                BrandId = brand.Id, Locale = "pt", Name = $"Marca {i:00}",
                Description = $"Fabricante de iscas nº {i:00}.",
            });
            brands.Add(brand);
        }

        var species = new List<Species>();
        var ptNames = new[]
        {
            "Robalo", "Sargo", "Dourada", "Achigã", "Lúcio", "Truta", "Pregado", "Linguado",
            "Corvina", "Garoupa", "Pargo", "Faneca", "Carpa", "Perca", "Espadarte", "Atum",
            "Cavala", "Carapau", "Salmão", "Enguia",
        };
        for (var i = 0; i < 20; i++)
        {
            var sp = new Species
            {
                Id = Guid.NewGuid(),
                Slug = $"especie-{i + 1:00}",
                Family = "Generic",
                WaterType = WaterTypes[i % WaterTypes.Length],
            };
            sp.Translations.Add(new SpeciesTranslation
            {
                SpeciesId = sp.Id, Locale = "pt", CommonName = ptNames[i],
            });
            species.Add(sp);
        }

        var lures = new List<Lure>();
        for (var i = 1; i <= 50; i++)
        {
            var brand = brands[i % brands.Count];
            var lure = new Lure
            {
                Id = Guid.NewGuid(),
                Slug = $"isca-{i:000}",
                BrandId = brand.Id,
                ModelRef = $"MR-{i:000}",
                LureType = LureTypes[i % LureTypes.Length],
                WaterType = WaterTypes[i % WaterTypes.Length],
                WeightG = 5 + i % 30,
                LengthMm = 40 + i % 80,
                DepthMinM = i % 3,
                DepthMaxM = 2 + i % 5,
                Status = "published",
            };
            lure.Translations.Add(new LureTranslation
            {
                LureId = lure.Id, Locale = "pt", Name = $"Isca {i:000}",
                Description = "Isca de exemplo para desenvolvimento.",
            });
            lures.Add(lure);
        }

        db.Brands.AddRange(brands);
        db.Species.AddRange(species);
        db.Lures.AddRange(lures);

        // F002 (T019): dados de seed têm origem 'automation' (todas as entidades auditáveis novas).
        foreach (var entry in db.ChangeTracker.Entries<IAuditable>())
            if (entry.State == EntityState.Added)
                entry.Entity.Source = AuditSource.Automation;

        await db.SaveChangesAsync(ct);
    }
}
