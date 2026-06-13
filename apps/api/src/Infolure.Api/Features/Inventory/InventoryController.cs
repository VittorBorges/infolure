using Infolure.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Infolure.Api.Features.Inventory;

[ApiController]
[Route("v1/me/inventory")]
[Authorize(Policy = AuthExtensions.UserPolicy)]
public class InventoryController(InventoryService inventory) : ControllerBase
{
    private string? Sub => User.FindFirst("sub")?.Value;

    /// <summary>US-06 — lista o inventário (agrupável por tipo no cliente).</summary>
    [HttpGet]
    public async Task<ActionResult<InventoryListResponse>> List(
        [FromQuery] string locale = "pt", CancellationToken ct = default)
    {
        if (Sub is null) return Unauthorized();
        var result = await inventory.ListAsync(Sub, locale, ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    /// <summary>US-06 — adiciona ao inventário.</summary>
    [HttpPost]
    public async Task<ActionResult<InventoryEntryDto>> Add([FromBody] AddInventoryRequest body, CancellationToken ct)
    {
        if (Sub is null) return Unauthorized();
        var (result, entry) = await inventory.AddAsync(Sub, body, ct);
        return result switch
        {
            InventoryResult.Ok => Created($"/v1/me/inventory/{entry!.Id}", entry),
            InventoryResult.Conflict => Conflict(),
            InventoryResult.LureNotFound => NotFound(),
            InventoryResult.Invalid => UnprocessableEntity(),
            _ => Unauthorized(),
        };
    }

    /// <summary>US-06 — atualiza quantidade/condição/notas.</summary>
    [HttpPatch("{entryId:guid}")]
    public async Task<ActionResult<InventoryEntryDto>> Update(
        Guid entryId, [FromBody] UpdateInventoryRequest body, CancellationToken ct)
    {
        if (Sub is null) return Unauthorized();
        var (result, entry) = await inventory.UpdateAsync(Sub, entryId, body, ct);
        return result switch
        {
            InventoryResult.Ok => Ok(entry),
            InventoryResult.NotOwner => Forbid(),
            InventoryResult.NotFound => NotFound(),
            InventoryResult.Invalid => UnprocessableEntity(),
            _ => Unauthorized(),
        };
    }

    /// <summary>US-06 — remove uma entrada.</summary>
    [HttpDelete("{entryId:guid}")]
    public async Task<IActionResult> Delete(Guid entryId, CancellationToken ct)
    {
        if (Sub is null) return Unauthorized();
        return await inventory.DeleteAsync(Sub, entryId, ct) switch
        {
            InventoryResult.Ok => NoContent(),
            InventoryResult.NotOwner => Forbid(),
            InventoryResult.NotFound => NotFound(),
            _ => Unauthorized(),
        };
    }
}
