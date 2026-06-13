using Infolure.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Infolure.Api.Features.Reviews;

[ApiController]
public class ReviewsController(ReviewsService reviews) : ControllerBase
{
    private string? Sub => User.FindFirst("sub")?.Value;

    /// <summary>US-08 — lista reviews + agregado (anónimo permitido; is_helpful = null sem sessão).</summary>
    [HttpGet("v1/lures/{slug}/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<ReviewsListResponse>> List(
        string slug, [FromQuery] string sort = "recent", CancellationToken ct = default)
    {
        var result = await reviews.ListAsync(slug, sort, Sub, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>US-08 — cria review (uma por utilizador/isca).</summary>
    [HttpPost("v1/lures/{slug}/reviews")]
    [Authorize(Policy = AuthExtensions.UserPolicy)]
    public async Task<ActionResult<ReviewDto>> Create(
        string slug, [FromBody] CreateReviewRequest body, CancellationToken ct)
    {
        if (Sub is null) return Unauthorized();
        var (result, review) = await reviews.CreateAsync(Sub, slug, body, ct);
        return result switch
        {
            ReviewResult.Ok => Created($"/v1/lures/{slug}/reviews/{review!.Id}", review),
            ReviewResult.Conflict => Conflict(),
            ReviewResult.LureNotFound => NotFound(),
            ReviewResult.Invalid => UnprocessableEntity(),
            _ => Unauthorized(),
        };
    }

    /// <summary>US-08 — edita a própria review.</summary>
    [HttpPatch("v1/lures/{slug}/reviews/{reviewId:guid}")]
    [Authorize(Policy = AuthExtensions.UserPolicy)]
    public async Task<ActionResult<ReviewDto>> Update(
        string slug, Guid reviewId, [FromBody] UpdateReviewRequest body, CancellationToken ct)
    {
        if (Sub is null) return Unauthorized();
        var (result, review) = await reviews.UpdateAsync(Sub, reviewId, body, ct);
        return result switch
        {
            ReviewResult.Ok => Ok(review),
            ReviewResult.NotOwner => Forbid(),
            ReviewResult.NotFound => NotFound(),
            ReviewResult.Invalid => UnprocessableEntity(),
            _ => Unauthorized(),
        };
    }

    /// <summary>US-08 — apaga a própria review.</summary>
    [HttpDelete("v1/lures/{slug}/reviews/{reviewId:guid}")]
    [Authorize(Policy = AuthExtensions.UserPolicy)]
    public async Task<IActionResult> Delete(string slug, Guid reviewId, CancellationToken ct)
    {
        if (Sub is null) return Unauthorized();
        return await reviews.DeleteAsync(Sub, reviewId, ct) switch
        {
            ReviewResult.Ok => NoContent(),
            ReviewResult.NotOwner => Forbid(),
            ReviewResult.NotFound => NotFound(),
            _ => Unauthorized(),
        };
    }

    /// <summary>US-08 — alterna o voto "útil" (um por utilizador/review).</summary>
    [HttpPost("v1/reviews/{reviewId:guid}/helpful")]
    [Authorize(Policy = AuthExtensions.UserPolicy)]
    public async Task<ActionResult<HelpfulResponse>> Helpful(Guid reviewId, CancellationToken ct)
    {
        if (Sub is null) return Unauthorized();
        var (result, response) = await reviews.ToggleHelpfulAsync(Sub, reviewId, ct);
        return result switch
        {
            ReviewResult.Ok => Ok(response),
            ReviewResult.NotFound => NotFound(),
            _ => Unauthorized(),
        };
    }
}
