using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Lifecycle;

/// <summary>
/// Cria/limpa dados de catálogo dedicados (isolados do seed e de testes paralelos).
/// A limpeza usa ExecuteDelete (IgnoreQueryFilters) para hard-delete real, contornando o
/// soft-delete do interceptor.
/// </summary>
internal static class LifecycleHelpers
{
    public static async Task<(Guid BrandId, Guid LureId)> CreateLureAsync(
        this CatalogApiFactory f, string suffix, Guid? targetSpeciesId = null)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var brand = new Brand { Id = Guid.NewGuid(), Slug = $"t-brand-{suffix}" };
        brand.Translations.Add(new BrandTranslation { BrandId = brand.Id, Locale = "pt", Name = $"T Brand {suffix}" });

        var lure = new Lure
        {
            Id = Guid.NewGuid(), Slug = $"t-lure-{suffix}", BrandId = brand.Id,
            LureType = "jig", Status = "published",
        };
        lure.Translations.Add(new LureTranslation { LureId = lure.Id, Locale = "pt", Name = $"T Lure {suffix}" });
        if (targetSpeciesId is { } sid)
            lure.TargetSpecies.Add(new LureTargetSpecies { LureId = lure.Id, SpeciesId = sid, Confidence = "primary" });

        db.Brands.Add(brand);
        db.Lures.Add(lure);
        await db.SaveChangesAsync();
        return (brand.Id, lure.Id);
    }

    public static async Task MutateAsync<T>(this CatalogApiFactory f, Guid id, Action<T> mutate)
        where T : class
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await db.Set<T>().IgnoreQueryFilters().FirstAsync(e => EF.Property<Guid>(e, "Id") == id);
        mutate(entity);
        await db.SaveChangesAsync();
    }

    public static async Task HardDeleteAsync(this CatalogApiFactory f, Guid brandId, Guid lureId)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Lure primeiro (cascata DB remove traduções/cores/imagens/target-species), depois a marca.
        await db.Lures.IgnoreQueryFilters().Where(l => l.Id == lureId).ExecuteDeleteAsync();
        await db.Brands.IgnoreQueryFilters().Where(b => b.Id == brandId).ExecuteDeleteAsync();
    }
}
