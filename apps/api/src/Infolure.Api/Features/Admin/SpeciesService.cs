using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Features.Admin;

/// <summary>
/// Feature 006 — get/update de espécie para o CRUD do backoffice. O create já existe
/// (AdminController.CreateSpecies); list/delete/restore/active usam o CRUD genérico.
/// Espelha o padrão de <see cref="BrandService"/>.
/// </summary>
public class SpeciesService(AppDbContext db)
{
    public enum Outcome { Ok, NotFound, SlugConflict }

    public async Task<SpeciesDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var s = await db.Species.Include(x => x.Translations).AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return null;
        var name = s.Translations.FirstOrDefault(t => t.Locale == "pt")?.CommonName ?? s.Slug;
        return new SpeciesDetailDto(s.Id, s.Slug, name, s.WaterType, s.Family);
    }

    public async Task<Outcome> UpdateAsync(Guid id, SpeciesWriteRequest req, CancellationToken ct)
    {
        var species = await db.Species.Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (species is null) return Outcome.NotFound;

        if (!string.IsNullOrWhiteSpace(req.Slug))
        {
            var slug = req.Slug.Trim();
            if (await db.Species.IgnoreQueryFilters().AnyAsync(x => x.Slug == slug && x.Id != id, ct))
                return Outcome.SlugConflict;
            species.Slug = slug;
        }

        species.WaterType = req.WaterType;
        species.Family = req.Family;

        var tr = species.Translations.FirstOrDefault(t => t.Locale == "pt");
        if (tr is null)
        {
            tr = new SpeciesTranslation { SpeciesId = species.Id, Locale = "pt" };
            species.Translations.Add(tr);
        }
        tr.CommonName = req.CommonName;

        await db.SaveChangesAsync(ct);
        return Outcome.Ok;
    }
}
