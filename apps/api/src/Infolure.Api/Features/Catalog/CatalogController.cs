using Microsoft.AspNetCore.Mvc;

namespace Infolure.Api.Features.Catalog;

[ApiController]
[Route("v1/lures")]
public class CatalogController(
    LureListService listService,
    SuggestService suggestService,
    LureDetailService detailService) : ControllerBase
{
    /// <summary>US-01/US-02 — listagem/busca com filtros, facets, ordenação e paginação.</summary>
    [HttpGet]
    public async Task<ActionResult<LureListResponse>> List(
        [FromQuery] string? q,
        [FromQuery(Name = "lure_type")] string? lureType,
        [FromQuery(Name = "water_type")] string? waterType,
        [FromQuery] string? species,
        [FromQuery] string? brand,
        [FromQuery(Name = "weight_min")] double? weightMin,
        [FromQuery(Name = "weight_max")] double? weightMax,
        [FromQuery(Name = "depth_min")] double? depthMin,
        [FromQuery(Name = "depth_max")] double? depthMax,
        [FromQuery] string sort = "popularity",
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int perPage = 20,
        [FromQuery] string locale = "pt",
        CancellationToken ct = default)
    {
        var query = new CatalogQuery
        {
            Q = q, LureType = lureType, WaterType = waterType, Species = species, Brand = brand,
            WeightMin = weightMin, WeightMax = weightMax, DepthMin = depthMin, DepthMax = depthMax,
            Sort = sort, Page = page, PerPage = perPage, Locale = locale,
        };
        return Ok(await listService.SearchAsync(query, ct));
    }

    /// <summary>US-02 — autocomplete (mín. 2 caracteres).</summary>
    [HttpGet("suggest")]
    public async Task<ActionResult<SuggestResponse>> Suggest(
        [FromQuery] string q, [FromQuery] string locale = "pt", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Ok(new SuggestResponse([]));
        return Ok(await suggestService.SuggestAsync(q.Trim(), locale, ct));
    }

    /// <summary>US-03 — detalhe por slug. 404 se não existir ou não publicada.</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<LureDetailDto>> Get(
        string slug, [FromQuery] string locale = "pt", CancellationToken ct = default)
    {
        var detail = await detailService.GetBySlugAsync(slug, locale, ct);
        return detail is null ? NotFound() : Ok(detail);
    }
}
