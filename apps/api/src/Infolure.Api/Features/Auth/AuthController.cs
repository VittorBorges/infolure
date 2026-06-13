using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Infolure.Api.Features.Auth;

[ApiController]
[Route("v1/auth")]
public class AuthController(AuthSyncService authService, IConfiguration config) : ControllerBase
{
    /// <summary>US-04 — webhook do Supabase (cria/atualiza utilizador). Protegido por segredo de webhook.</summary>
    [HttpPost("sync")]
    [AllowAnonymous]
    public async Task<ActionResult<SyncUserResponse>> Sync(
        [FromBody] SyncUserRequest body,
        [FromHeader(Name = "X-Webhook-Secret")] string? secret,
        CancellationToken ct)
    {
        var expected = config["Supabase:WebhookSecret"];
        if (!string.IsNullOrEmpty(expected) && secret != expected)
            return Unauthorized();

        return Ok(await authService.SyncAsync(body, ct));
    }

    /// <summary>US-04 — define o username no primeiro login OAuth.</summary>
    [HttpPost("username")]
    [Authorize(Policy = Infrastructure.Auth.AuthExtensions.UserPolicy)]
    public async Task<ActionResult<SetUsernameResponse>> SetUsername(
        [FromBody] SetUsernameRequest body, CancellationToken ct)
    {
        var sub = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(sub)) return Unauthorized();

        var (result, username) = await authService.SetUsernameAsync(sub, body.Username, ct);
        return result switch
        {
            SetUsernameResult.Ok => Ok(new SetUsernameResponse(username!)),
            SetUsernameResult.Taken => Conflict(),
            SetUsernameResult.Invalid => UnprocessableEntity(),
            _ => NotFound(),
        };
    }
}
