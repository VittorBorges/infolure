using Infolure.Api.Infrastructure.Auth;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Infolure.Api.Infrastructure.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Features.Admin;

[ApiController]
[Route("v1/admin")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class AdminController(AppDbContext db, RetailerPriceService prices, LureIndexer indexer) : ControllerBase
{
    // T079 — marcas
    [HttpPost("brands")]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrandRequest body, CancellationToken ct)
    {
        var brand = new Brand { Id = Guid.NewGuid(), Slug = body.Slug };
        brand.Translations.Add(new BrandTranslation { BrandId = brand.Id, Locale = "pt", Name = body.Name });
        db.Brands.Add(brand);
        await db.SaveChangesAsync(ct);
        return Created($"/v1/admin/brands/{brand.Id}", new { id = brand.Id });
    }

    // T079 — espécies
    [HttpPost("species")]
    public async Task<IActionResult> CreateSpecies([FromBody] CreateSpeciesRequest body, CancellationToken ct)
    {
        var species = new Species { Id = Guid.NewGuid(), Slug = body.Slug, WaterType = body.WaterType };
        species.Translations.Add(new SpeciesTranslation { SpeciesId = species.Id, Locale = "pt", CommonName = body.CommonName });
        db.Species.Add(species);
        await db.SaveChangesAsync(ct);
        return Created($"/v1/admin/species/{species.Id}", new { id = species.Id });
    }

    // T078 — iscas (create)
    [HttpPost("lures")]
    public async Task<IActionResult> CreateLure([FromBody] CreateLureRequest body, CancellationToken ct)
    {
        var lure = new Lure
        {
            Id = Guid.NewGuid(),
            Slug = body.Slug,
            LureType = body.LureType,
            BrandId = body.BrandId,
            Status = body.Status ?? "draft",
        };
        lure.Translations.Add(new LureTranslation { LureId = lure.Id, Locale = "pt", Name = body.Name });
        db.Lures.Add(lure);
        await db.SaveChangesAsync(ct);
        if (lure.Status == "published")
            try { await indexer.ReindexLureAsync(lure.Id, ct); } catch { /* best-effort */ }
        return Created($"/v1/admin/lures/{lure.Id}", new { id = lure.Id });
    }

    // T078 — iscas (update)
    [HttpPatch("lures/{id:guid}")]
    public async Task<IActionResult> UpdateLure(Guid id, [FromBody] UpdateLureRequest body, CancellationToken ct)
    {
        var lure = await db.Lures.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lure is null) return NotFound();
        if (body.Status is not null) lure.Status = body.Status;
        if (body.WeightG is not null) lure.WeightG = body.WeightG;
        await db.SaveChangesAsync(ct);
        try { await indexer.ReindexLureAsync(id, ct); } catch { /* best-effort */ }
        return NoContent();
    }

    // T080 — preços de retalhistas (recalcula price_6m_*)
    [HttpPost("lures/{id:guid}/prices")]
    public async Task<ActionResult<PriceSummary>> AddPrice(Guid id, [FromBody] AddRetailerPriceRequest body, CancellationToken ct)
    {
        var summary = await prices.AddPriceAsync(id, body, ct);
        return summary is null ? NotFound() : Ok(summary);
    }

    // T081 — moderação de reviews (hide/show)
    [HttpPatch("reviews/{reviewId:guid}/moderation")]
    public async Task<IActionResult> ModerateReview(Guid reviewId, [FromBody] ModerateReviewRequest body, CancellationToken ct)
    {
        if (body.Status is not ("published" or "hidden")) return UnprocessableEntity();
        var review = await db.LureReviews.FirstOrDefaultAsync(r => r.Id == reviewId, ct);
        if (review is null) return NotFound();
        review.Status = body.Status;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
