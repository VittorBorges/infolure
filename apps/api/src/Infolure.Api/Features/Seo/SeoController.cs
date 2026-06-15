using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Infolure.Api.Features.Seo;

/// <summary>T040 (US-03): estado de indexação para o robots.ts/sitemap.ts do frontend (público).</summary>
[ApiController]
[Route("v1/seo")]
[AllowAnonymous]
public class SeoController(SeoSettingsService seo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var enabled = await seo.GetEnabledAsync(ct);
        var sitemap = await seo.GetSitemapAsync(ct);
        return Ok(new
        {
            indexing_enabled = enabled,
            sitemap = sitemap.Select(s => new { slug = s.Slug, updated_at = s.UpdatedAt }),
        });
    }
}
