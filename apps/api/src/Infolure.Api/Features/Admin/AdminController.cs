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
    LureWriteService lureWrite,
    BrandService brands,
    SpeciesService species,
    FluentValidation.IValidator<LureWriteRequest> lureValidator,
    Infolure.Api.Features.Media.BlobUploadService blobUpload,
    Infolure.Api.Features.Seo.SeoSettingsService seo,
    Infolure.Api.Features.Users.ProfileService profiles,
    IAdminActionContext adminCtx,
    IServiceProvider sp) : ControllerBase
{
    private static readonly string[] KnownResources = ["lures", "brands", "species", "users"];

    // ---- Dashboard (US-02 / FR-008) ----
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct) => Ok(await dashboard.GetAsync(ct));

    // ---- Consulta do registo de auditoria (US-04 / FR-021) ----
    [HttpGet("audit")]
    public async Task<IActionResult> Audit(
        [FromQuery] Guid? actor, [FromQuery] string? action,
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1, [FromQuery(Name = "per_page")] int perPage = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        perPage = Math.Clamp(perPage <= 0 ? 20 : perPage, 1, 100);

        var q = db.AdminAuditLog.AsQueryable();
        if (actor is { } a) q = q.Where(e => e.ActorUserId == a);
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(e => e.Action == action);
        if (from is { } f) q = q.Where(e => e.CreatedAt >= f);
        if (to is { } t) q = q.Where(e => e.CreatedAt <= t);

        var total = await q.CountAsync(ct);
        var data = await q.OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * perPage).Take(perPage)
            .Select(e => new
            {
                id = e.Id, actor_user_id = e.ActorUserId, action = e.Action,
                entity_type = e.EntityType, entity_id = e.EntityId,
                is_personal_data = e.IsPersonalData, changes = e.Changes, created_at = e.CreatedAt,
            })
            .ToListAsync(ct);

        return Ok(new { data, meta = new { total, page, per_page = perPage } });
    }

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
                    lure_type = l.LureType, status = l.Status,
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
                var sq = db.Species.Include(s => s.Translations).AsQueryable();
                if (!string.IsNullOrWhiteSpace(q)) sq = sq.Where(s => s.Slug.Contains(q) || s.Translations.Any(t => t.CommonName.Contains(q)));
                return Ok(await resources.ListAsync(sq, query, s => new
                {
                    id = s.Id, slug = s.Slug,
                    name = s.Translations.FirstOrDefault(t => t.Locale == "pt")!.CommonName,
                    water_type = s.WaterType, family = s.Family,
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

    // ---- Feature 006 (US2) — get/update de marca (CRUD) ----
    [HttpGet("brands/{id:guid}")]
    public async Task<IActionResult> GetBrand(Guid id, CancellationToken ct)
    {
        var dto = await brands.GetAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("brands/{id:guid}")]
    public async Task<IActionResult> UpdateBrand(Guid id, [FromBody] BrandWriteRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name)) return UnprocessableEntity(new { error = "nome obrigatório" });
        var outcome = await brands.UpdateAsync(id, body, ct);
        return outcome switch
        {
            BrandService.Outcome.NotFound => NotFound(),
            BrandService.Outcome.SlugConflict => Conflict(new { error = "slug em conflito", body.Slug }),
            _ => NoContent(),
        };
    }

    [HttpPost("species")]
    public async Task<IActionResult> CreateSpecies([FromBody] CreateSpeciesRequest body, CancellationToken ct)
    {
        var sp = new Species { Id = Guid.NewGuid(), Slug = body.Slug, WaterType = body.WaterType, Family = body.Family };
        sp.Translations.Add(new SpeciesTranslation { SpeciesId = sp.Id, Locale = "pt", CommonName = body.CommonName });
        db.Species.Add(sp);
        await db.SaveChangesAsync(ct);
        return Created($"/v1/admin/species/{sp.Id}", new { id = sp.Id });
    }

    // ---- Feature 006 — get/update de espécie (CRUD) ----
    [HttpGet("species/{id:guid}")]
    public async Task<IActionResult> GetSpecies(Guid id, CancellationToken ct)
    {
        var dto = await species.GetAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("species/{id:guid}")]
    public async Task<IActionResult> UpdateSpecies(Guid id, [FromBody] SpeciesWriteRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.CommonName)) return UnprocessableEntity(new { error = "nome comum obrigatório" });
        var outcome = await species.UpdateAsync(id, body, ct);
        return outcome switch
        {
            SpeciesService.Outcome.NotFound => NotFound(),
            SpeciesService.Outcome.SlugConflict => Conflict(new { error = "slug em conflito", body.Slug }),
            _ => NoContent(),
        };
    }

    // Validação FluentValidation → 422 ProblemDetails com mapa campo→mensagens.
    private async Task<IActionResult?> ValidateLure(LureWriteRequest body, CancellationToken ct)
    {
        var result = await lureValidator.ValidateAsync(body, ct);
        if (result.IsValid) return null;
        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return UnprocessableEntity(new ValidationProblemDetails(errors) { Title = "validação falhou" });
    }

    // ---- Feature 005 — registo/edição completos de iscas (US1/US2) ----

    [HttpPost("lures")]
    public async Task<IActionResult> CreateLure([FromBody] LureWriteRequest body, CancellationToken ct)
    {
        if (await ValidateLure(body, ct) is { } problem) return problem;
        var r = await lureWrite.CreateAsync(body, ct);
        return r.Outcome switch
        {
            LureWriteService.Outcome.SlugConflict => Conflict(new { error = "slug já existente", body.Slug }),
            _ => Created($"/v1/admin/lures/{r.Id}", new { id = r.Id }),
        };
    }

    [HttpGet("lures/{id:guid}")]
    public async Task<IActionResult> GetLure(Guid id, CancellationToken ct)
    {
        var dto = await lureWrite.GetForEditAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("lures/{id:guid}")]
    public async Task<IActionResult> UpdateLure(Guid id, [FromBody] LureWriteRequest body, CancellationToken ct)
    {
        if (await ValidateLure(body, ct) is { } problem) return problem;
        var outcome = await lureWrite.UpdateAsync(id, body, ct);
        return outcome switch
        {
            LureWriteService.Outcome.NotFound => NotFound(),
            LureWriteService.Outcome.SlugConflict => Conflict(new { error = "slug em conflito", body.Slug }),
            _ => NoContent(),
        };
    }

    // ---- Feature 005 — upload de foto (foto de cor, US3) ----
    [HttpPost("media")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadMedia(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { error = "ficheiro em falta" });
        await using var stream = file.OpenReadStream();
        var r = await blobUpload.UploadAsync(stream, file.ContentType, file.Length, file.FileName, ct);
        return r.Outcome switch
        {
            Infolure.Api.Features.Media.BlobUploadService.Outcome.UnsupportedType => StatusCode(415, new { error = "tipo de ficheiro não suportado" }),
            Infolure.Api.Features.Media.BlobUploadService.Outcome.TooLarge => StatusCode(413, new { error = "ficheiro demasiado grande" }),
            Infolure.Api.Features.Media.BlobUploadService.Outcome.NotConfigured => StatusCode(503, new { error = "armazenamento de média não configurado" }),
            _ => Created(r.Url!, new { url = r.Url }),
        };
    }

    [HttpPost("lures/{id:guid}/prices")]
    public async Task<ActionResult<PriceSummary>> AddPrice(Guid id, [FromBody] AddRetailerPriceRequest body, CancellationToken ct)
    {
        var summary = await prices.AddPriceAsync(id, body, ct);
        return summary is null ? NotFound() : Ok(summary);
    }

    // ---- Eliminação RGPD efetiva (FR-012a / T036b) ----
    // Distinta do soft-delete: anonimiza PII e remove vínculos de auth de forma IRREVERSÍVEL.
    [HttpPost("users/{id:guid}/erase")]
    public async Task<IActionResult> EraseUser(Guid id, CancellationToken ct)
    {
        if (Guard(id) is { } g) return g; // não a própria conta nem o último admin
        var ok = await profiles.EraseUserAsync(id, "admin", ct);
        if (!ok) return NotFound();
        await AfterMutation("users", id, ct); // invalida cache de estado
        return NoContent();
    }

    // ---- Indexação SEO GLOBAL (Feature 006/US1 — único controlo; sem opção por isca) ----
    public record IndexingRequest(bool Enabled);

    [HttpGet("settings/indexing")]
    public async Task<IActionResult> GetIndexing(CancellationToken ct)
        => Ok(new { enabled = await seo.GetEnabledAsync(ct) });

    [HttpPut("settings/indexing")]
    public async Task<IActionResult> SetIndexing([FromBody] IndexingRequest body, CancellationToken ct)
    {
        await seo.SetEnabledAsync(body.Enabled, adminCtx.ActorUserId, ct);
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
