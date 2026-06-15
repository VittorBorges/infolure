using Infolure.Api.Infrastructure.Auth;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Auditing;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Infolure.Api.Infrastructure.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Infolure.Api.Features.Admin;

[ApiController]
[Route("v1/admin")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class AdminController(
    AppDbContext db,
    RetailerPriceService prices,
    LureIndexer indexer,
    AdminResourceService resources,
    DashboardService dashboard,
    Infolure.Api.Features.Seo.SeoSettingsService seo,
    IAdminActionContext adminCtx,
    IServiceProvider sp) : ControllerBase
{
    private static readonly string[] KnownResources = ["lures", "brands", "species", "users"];

    // ---- Dashboard (US-02 / FR-008) ----
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct) => Ok(await dashboard.GetAsync(ct));

    // ---- Listagem genérica por recurso (FR-010) ----
    [HttpGet("{resource}")]
    public async Task<IActionResult> List(string resource, [FromQuery] string? q, [FromQuery(Name = "is_active")] bool? isActive,
        [FromQuery] string? source, [FromQuery] string? include, [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int perPage = 20, CancellationToken ct = default)
    {
        var query = new AdminResourceService.ListQuery(q, isActive, source, include, page, perPage);
        switch (resource)
        {
            case "lures":
                var lq = db.Lures.Include(l => l.Translations).AsQueryable();
                if (!string.IsNullOrWhiteSpace(q)) lq = lq.Where(l => l.Slug.Contains(q) || l.Translations.Any(t => t.Name.Contains(q)));
                return Ok(await resources.ListAsync(lq, query, l => new
                {
                    id = l.Id, slug = l.Slug, name = l.Translations.FirstOrDefault(t => t.Locale == "pt")!.Name,
                    lure_type = l.LureType, status = l.Status, is_indexable = l.IsIndexable,
                    is_active = l.IsActive, source = l.Source, deleted_at = l.DeletedAt,
                }, ct));
            case "brands":
                var bq = db.Brands.Include(b => b.Translations).AsQueryable();
                if (!string.IsNullOrWhiteSpace(q)) bq = bq.Where(b => b.Slug.Contains(q) || b.Translations.Any(t => t.Name.Contains(q)));
                return Ok(await resources.ListAsync(bq, query, b => new
                {
                    id = b.Id, slug = b.Slug, name = b.Translations.FirstOrDefault(t => t.Locale == "pt")!.Name,
                    is_active = b.IsActive, source = b.Source, deleted_at = b.DeletedAt,
                }, ct));
            case "species":
                var sq = db.Species.AsQueryable();
                if (!string.IsNullOrWhiteSpace(q)) sq = sq.Where(s => s.Slug.Contains(q));
                return Ok(await resources.ListAsync(sq, query, s => new
                {
                    id = s.Id, slug = s.Slug, water_type = s.WaterType,
                    is_active = s.IsActive, source = s.Source, deleted_at = s.DeletedAt,
                }, ct));
            case "users":
                var uq = db.Users.AsQueryable();
                if (!string.IsNullOrWhiteSpace(q)) uq = uq.Where(u => (u.Username ?? "").Contains(q) || (u.Email ?? "").Contains(q));
                return Ok(await resources.ListAsync(uq, query, u => new
                {
                    id = u.Id, username = u.Username, email = u.Email, role = u.Role,
                    is_active = u.IsActive, source = u.Source, deleted_at = u.DeletedAt, created_at = u.CreatedAt,
                }, ct));
            default:
                return NotFound(new { error = "recurso desconhecido", resource });
        }
    }

    [HttpDelete("{resource}/{id:guid}")]
    public async Task<IActionResult> Delete(string resource, Guid id, CancellationToken ct)
    {
        if (!KnownResources.Contains(resource)) return NotFound();
        if (resource == "users" && Guard(id) is { } g) return g;

        var ok = resource switch
        {
            "lures" => await resources.DeleteAsync<Lure>(id, ct),
            "brands" => await resources.DeleteAsync<Brand>(id, ct),
            "species" => await resources.DeleteAsync<Species>(id, ct),
            "users" => await resources.DeleteAsync<User>(id, ct),
            _ => false,
        };
        if (!ok) return NotFound();
        await AfterMutation(resource, id, ct);
        return NoContent();
    }

    [HttpPost("{resource}/{id:guid}/restore")]
    public async Task<IActionResult> Restore(string resource, Guid id, CancellationToken ct)
    {
        var ok = resource switch
        {
            "lures" => await resources.RestoreAsync<Lure>(id, ct),
            "brands" => await resources.RestoreAsync<Brand>(id, ct),
            "species" => await resources.RestoreAsync<Species>(id, ct),
            "users" => await resources.RestoreAsync<User>(id, ct),
            _ => (bool?)null,
        } ?? throw new InvalidOperationException();
        if (!ok) return NotFound();
        await AfterMutation(resource, id, ct);
        return NoContent();
    }

    public record SetActiveRequest(bool IsActive);

    [HttpPut("{resource}/{id:guid}/active")]
    public async Task<IActionResult> SetActive(string resource, Guid id, [FromBody] SetActiveRequest body, CancellationToken ct)
    {
        if (!KnownResources.Contains(resource)) return NotFound();
        if (resource == "users" && !body.IsActive && Guard(id) is { } g) return g;

        var ok = resource switch
        {
            "lures" => await resources.SetActiveAsync<Lure>(id, body.IsActive, ct),
            "brands" => await resources.SetActiveAsync<Brand>(id, body.IsActive, ct),
            "species" => await resources.SetActiveAsync<Species>(id, body.IsActive, ct),
            "users" => await resources.SetActiveAsync<User>(id, body.IsActive, ct),
            _ => false,
        };
        if (!ok) return NotFound();
        await AfterMutation(resource, id, ct);
        return NoContent();
    }

    // Salvaguardas (FR-013): não desativar/eliminar a própria conta nem o último admin.
    private IActionResult? Guard(Guid targetUserId)
    {
        if (adminCtx.ActorUserId == targetUserId)
            return Conflict(new { error = "não pode desativar/eliminar a própria conta" });

        var target = db.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Id == targetUserId);
        if (target is { Role: "admin" })
        {
            var activeAdmins = db.Users.Count(u => u.Role == "admin" && u.IsActive && u.DeletedAt == null);
            if (activeAdmins <= 1)
                return Conflict(new { error = "não pode remover o último administrador" });
        }
        return null;
    }

    // Efeitos colaterais pós-mutação: reindexar iscas; invalidar cache de estado de utilizador.
    private async Task AfterMutation(string resource, Guid id, CancellationToken ct)
    {
        if (resource == "lures")
            try { await indexer.ReindexLureAsync(id, ct); } catch { /* best-effort */ }

        if (resource == "users")
        {
            var redis = sp.GetService<IConnectionMultiplexer>();
            if (redis is not null)
            {
                var subs = await db.UserAuthProviders.IgnoreQueryFilters()
                    .Where(p => p.UserId == id).Select(p => p.ProviderUid).ToListAsync(ct);
                var cache = redis.GetDatabase();
                foreach (var s in subs) await cache.KeyDeleteAsync($"user:state:{s}");
            }
        }
    }

    // ===================== Endpoints específicos (efeitos de domínio) =====================

    [HttpPost("brands")]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrandRequest body, CancellationToken ct)
    {
        var brand = new Brand { Id = Guid.NewGuid(), Slug = body.Slug };
        brand.Translations.Add(new BrandTranslation { BrandId = brand.Id, Locale = "pt", Name = body.Name });
        db.Brands.Add(brand);
        await db.SaveChangesAsync(ct);
        return Created($"/v1/admin/brands/{brand.Id}", new { id = brand.Id });
    }

    [HttpPost("species")]
    public async Task<IActionResult> CreateSpecies([FromBody] CreateSpeciesRequest body, CancellationToken ct)
    {
        var species = new Species { Id = Guid.NewGuid(), Slug = body.Slug, WaterType = body.WaterType };
        species.Translations.Add(new SpeciesTranslation { SpeciesId = species.Id, Locale = "pt", CommonName = body.CommonName });
        db.Species.Add(species);
        await db.SaveChangesAsync(ct);
        return Created($"/v1/admin/species/{species.Id}", new { id = species.Id });
    }

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

    [HttpPost("lures/{id:guid}/prices")]
    public async Task<ActionResult<PriceSummary>> AddPrice(Guid id, [FromBody] AddRetailerPriceRequest body, CancellationToken ct)
    {
        var summary = await prices.AddPriceAsync(id, body, ct);
        return summary is null ? NotFound() : Ok(summary);
    }

    // ---- Controlo de indexação (US-03 / FR-014) ----
    public record IndexingRequest(bool Enabled);

    [HttpPut("settings/indexing")]
    public async Task<IActionResult> SetIndexing([FromBody] IndexingRequest body, CancellationToken ct)
    {
        await seo.SetEnabledAsync(body.Enabled, adminCtx.ActorUserId, ct);
        return NoContent();
    }

    public record IndexableRequest(bool IsIndexable);

    [HttpPut("lures/{id:guid}/indexable")]
    public async Task<IActionResult> SetLureIndexable(Guid id, [FromBody] IndexableRequest body, CancellationToken ct)
    {
        var lure = await db.Lures.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lure is null) return NotFound();
        lure.IsIndexable = body.IsIndexable; // só afeta sitemap/meta SEO, não a busca Typesense
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

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
