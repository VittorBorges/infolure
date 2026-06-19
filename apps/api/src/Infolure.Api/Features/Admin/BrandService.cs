using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Features.Admin;

/// <summary>
/// Feature 006 (US2) — get/update de marca para o CRUD do backoffice. O create já existe
/// (AdminController.CreateBrand); list/delete/restore/active usam o CRUD genérico.
/// </summary>
public class BrandService(AppDbContext db)
{
    public enum Outcome { Ok, NotFound, SlugConflict }

    public async Task<BrandDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var b = await db.Brands.Include(x => x.Translations).AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (b is null) return null;
        var name = b.Translations.FirstOrDefault(t => t.Locale == "pt")?.Name ?? b.Slug;
        return new BrandDetailDto(b.Id, b.Slug, name);
    }

    public async Task<Outcome> UpdateAsync(Guid id, BrandWriteRequest req, CancellationToken ct)
    {
        var brand = await db.Brands.Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (brand is null) return Outcome.NotFound;

        if (!string.IsNullOrWhiteSpace(req.Slug))
        {
            var slug = req.Slug.Trim();
            if (await db.Brands.IgnoreQueryFilters().AnyAsync(x => x.Slug == slug && x.Id != id, ct))
                return Outcome.SlugConflict;
            brand.Slug = slug;
        }

        var tr = brand.Translations.FirstOrDefault(t => t.Locale == "pt");
        if (tr is null)
        {
            tr = new BrandTranslation { BrandId = brand.Id, Locale = "pt" };
            brand.Translations.Add(tr);
        }
        tr.Name = req.Name;

        await db.SaveChangesAsync(ct);
        return Outcome.Ok;
    }
}
