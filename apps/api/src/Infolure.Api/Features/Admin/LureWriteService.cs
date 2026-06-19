using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Infolure.Api.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Features.Admin;

/// <summary>
/// Feature 005/006 — escrita transacional de iscas (criar/editar) com coleções aninhadas.
/// Estratégia replace-children ESTRITAMENTE limitada a translations(pt)/configurations/colors(+hex)/
/// imagens-de-cor/target-species. NUNCA toca em preços, reviews nem imagens gerais.
/// Na edição, status ausente preserva o atual (FR-013). lure_configurations é a fonte única de
/// peso/comprimento e do anzol (Feature 006); cada cor pode ter várias fotos.
/// </summary>
public class LureWriteService(AppDbContext db, LureIndexer indexer, ILogger<LureWriteService> log)
{
    public enum Outcome { Ok, NotFound, SlugConflict }

    public record WriteResult(Outcome Outcome, Guid Id);

    public async Task<WriteResult> CreateAsync(LureWriteRequest req, CancellationToken ct)
    {
        if (await SlugTakenAsync(req.Slug, null, ct))
            return new WriteResult(Outcome.SlugConflict, Guid.Empty);

        var lure = new Lure
        {
            Id = Guid.NewGuid(),
            Slug = req.Slug.Trim(),
            Status = req.Status ?? "draft",
        };
        ApplyScalars(lure, req);
        db.Lures.Add(lure);
        ApplyTranslation(lure, req);
        ApplyChildren(lure, req);

        await db.SaveChangesAsync(ct);
        await SafeReindexAsync(lure.Id, ct);
        log.LogInformation("Lure created {LureId} slug={Slug}", lure.Id, lure.Slug);
        return new WriteResult(Outcome.Ok, lure.Id);
    }

    public async Task<Outcome> UpdateAsync(Guid id, LureWriteRequest req, CancellationToken ct)
    {
        var lure = await db.Lures
            .Include(l => l.Translations)
            .Include(l => l.Configurations)
            .Include(l => l.Colors)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lure is null) return Outcome.NotFound;
        if (await SlugTakenAsync(req.Slug, id, ct)) return Outcome.SlugConflict;

        lure.Slug = req.Slug.Trim();
        ApplyScalars(lure, req);
        if (req.Status is not null) lure.Status = req.Status;   // ausente → preserva (FR-013)
        lure.UpdatedAt = DateTimeOffset.UtcNow;
        ApplyTranslation(lure, req);

        // replace-children limitado: configurations, colors(+hex via owned json) e imagens DE COR.
        db.LureConfigurations.RemoveRange(lure.Configurations);
        db.LureColors.RemoveRange(lure.Colors);
        var colorImages = await db.LureImages.Where(i => i.LureId == id && i.ColorId != null).ToListAsync(ct);
        db.LureImages.RemoveRange(colorImages);
        db.LureTargetSpecies.RemoveRange(db.LureTargetSpecies.Where(t => t.LureId == id));

        ApplyChildren(lure, req);

        await db.SaveChangesAsync(ct);
        await SafeReindexAsync(id, ct);
        log.LogInformation("Lure updated {LureId} slug={Slug}", id, lure.Slug);
        return Outcome.Ok;
    }

    public async Task<AdminLureDetailDto?> GetForEditAsync(Guid id, CancellationToken ct)
    {
        var l = await db.Lures
            .Include(x => x.Translations)
            .Include(x => x.Configurations)
            .Include(x => x.Colors)
            .Include(x => x.Images)
            .Include(x => x.TargetSpecies).ThenInclude(t => t.Species).ThenInclude(s => s.Translations)
            .Include(x => x.Brand).ThenInclude(b => b!.Translations)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (l is null) return null;

        var tr = l.Translations.FirstOrDefault(t => t.Locale == "pt");
        var brandName = l.Brand?.Translations.FirstOrDefault(t => t.Locale == "pt")?.Name;
        // Feature 006 — várias fotos por cor (ordenadas), agrupadas por color_id.
        var photosByColor = l.Images.Where(i => i.ColorId != null)
            .GroupBy(i => i.ColorId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
                .Select(i => i.Url).ToList());

        return new AdminLureDetailDto(
            l.Id, l.Slug, tr?.Name ?? l.Slug, tr?.Description, l.BrandId, brandName, l.LureType, l.WaterType,
            l.ModelRef, l.Material, l.DepthMinM, l.DepthMaxM, l.Status,
            l.Configurations.OrderBy(c => c.SortOrder)
                .Select(c => new AdminLureConfigurationDto(c.Id, c.Code, c.Label, c.LengthMm, c.WeightG,
                    c.HookSize, c.HookType, c.HookCount, c.SortOrder)).ToList(),
            l.Colors.Select(c => new AdminLureColorDto(
                c.Id, c.NamePt, c.NameEn, c.Pattern,
                photosByColor.GetValueOrDefault(c.Id) ?? [],
                c.HexCodes.Select(h => new AdminLureHexCodeDto(h.Hex, h.Label, 0)).ToList())).ToList(),
            l.TargetSpecies.Select(t => new TargetSpeciesDetailDto(
                t.SpeciesId,
                t.Species.Translations.FirstOrDefault(tr => tr.Locale == "pt")?.CommonName ?? t.Species.Slug,
                t.Confidence)).ToList());
    }

    private async Task<bool> SlugTakenAsync(string slug, Guid? exceptId, CancellationToken ct)
        => await db.Lures.IgnoreQueryFilters()
            .AnyAsync(l => l.Slug == slug.Trim() && (exceptId == null || l.Id != exceptId), ct);

    private static void ApplyScalars(Lure lure, LureWriteRequest req)
    {
        lure.BrandId = req.BrandId;
        lure.LureType = req.LureType;
        lure.WaterType = req.WaterType;
        lure.ModelRef = req.ModelRef;
        lure.Material = req.Material;
        lure.DepthMinM = req.DepthMinM;
        lure.DepthMaxM = req.DepthMaxM;
        // Feature 006: anzol agora por configuração; indexação SEO é global (sem campos na isca).
    }

    private static void ApplyTranslation(Lure lure, LureWriteRequest req)
    {
        var tr = lure.Translations.FirstOrDefault(t => t.Locale == "pt");
        if (tr is null)
        {
            tr = new LureTranslation { LureId = lure.Id, Locale = "pt" };
            lure.Translations.Add(tr);
        }
        tr.Name = req.Name;
        tr.Description = req.Description;
    }

    private void ApplyChildren(Lure lure, LureWriteRequest req)
    {
        foreach (var (cfg, i) in (req.Configurations ?? []).Select((c, i) => (c, i)))
            db.LureConfigurations.Add(new LureConfiguration
            {
                Id = Guid.NewGuid(), LureId = lure.Id, Code = cfg.Code, Label = cfg.Label,
                LengthMm = cfg.LengthMm, WeightG = cfg.WeightG,
                HookSize = cfg.HookSize, HookType = cfg.HookType, HookCount = cfg.HookCount,
                SortOrder = cfg.SortOrder == 0 ? (short)i : cfg.SortOrder,
            });

        foreach (var c in req.Colors ?? [])
        {
            var colorId = Guid.NewGuid();
            db.LureColors.Add(new LureColor
            {
                Id = colorId, LureId = lure.Id, NamePt = c.NamePt ?? "", NameEn = c.NameEn, Pattern = c.Pattern,
                HexCodes = (c.HexCodes ?? [])
                    .OrderBy(h => h.SortOrder)
                    .Select(h => new LureHexCode { Hex = h.Hex.Trim().ToLowerInvariant(), Label = h.Label })
                    .ToList(),
            });
            // Feature 006 — várias fotos por cor (ordem = índice na lista).
            foreach (var (url, idx) in (c.PhotoUrls ?? []).Where(u => !string.IsNullOrWhiteSpace(u)).Select((u, idx) => (u, idx)))
                db.LureImages.Add(new LureImage
                {
                    Id = Guid.NewGuid(), LureId = lure.Id, ColorId = colorId, Url = url, SortOrder = (short)idx, IsPrimary = idx == 0,
                });
        }

        foreach (var t in req.TargetSpecies ?? [])
            db.LureTargetSpecies.Add(new LureTargetSpecies
            {
                LureId = lure.Id, SpeciesId = t.SpeciesId, Confidence = t.Confidence,
            });
    }

    private async Task SafeReindexAsync(Guid id, CancellationToken ct)
    {
        try { await indexer.ReindexLureAsync(id, ct); }
        catch (Exception ex) { log.LogWarning(ex, "Reindex best-effort falhou para {LureId}", id); }
    }
}
