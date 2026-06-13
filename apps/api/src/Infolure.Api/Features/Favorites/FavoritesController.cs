using Infolure.Api.Features.Catalog;
using Infolure.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Infolure.Api.Features.Favorites;

[ApiController]
[Route("v1/me/favorites")]
[Authorize(Policy = AuthExtensions.UserPolicy)]
public class FavoritesController(FavoritesService favorites) : ControllerBase
{
    private string? Sub => User.FindFirst("sub")?.Value;

    /// <summary>US-05 — lista os favoritos do utilizador (cards, paginado).</summary>
    [HttpGet]
    public async Task<ActionResult<LureListResponse>> List(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int perPage = 20,
        [FromQuery] string locale = "pt",
        CancellationToken ct = default)
    {
        if (Sub is null) return Unauthorized();
        var result = await favorites.ListAsync(Sub, page, perPage, locale, ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    /// <summary>US-05 — adiciona favorito.</summary>
    [HttpPost("{lureId:guid}")]
    public async Task<IActionResult> Add(Guid lureId, CancellationToken ct)
    {
        if (Sub is null) return Unauthorized();
        return await favorites.AddAsync(Sub, lureId, ct) switch
        {
            FavoriteResult.Ok => NoContent(),
            FavoriteResult.LureNotFound => NotFound(),
            _ => Unauthorized(),
        };
    }

    /// <summary>US-05 — remove favorito.</summary>
    [HttpDelete("{lureId:guid}")]
    public async Task<IActionResult> Remove(Guid lureId, CancellationToken ct)
    {
        if (Sub is null) return Unauthorized();
        return await favorites.RemoveAsync(Sub, lureId, ct) switch
        {
            FavoriteResult.Ok => NoContent(),
            _ => Unauthorized(),
        };
    }
}
