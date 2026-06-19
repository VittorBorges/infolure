using System.Security.Claims;
using Infolure.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Infolure.Api.Features.Users;

[ApiController]
public class ProfileController(ProfileService profiles) : ControllerBase
{
    // O JwtBearer (MapInboundClaims=true, default) renomeia o claim `sub` do Supabase para
    // ClaimTypes.NameIdentifier; daí o fallback (mesmo padrão do ActiveUserMiddleware).
    private string? Sub => User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>US-07 — perfil público (sem PII).</summary>
    [HttpGet("v1/users/{username}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicProfileDto>> Get(string username, CancellationToken ct)
    {
        var profile = await profiles.GetPublicProfileAsync(username, ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    /// <summary>Feature 007 — identidade da sessão atual (nome/email + função) para o painel admin.</summary>
    [HttpGet("v1/me")]
    [Authorize(Policy = AuthExtensions.UserPolicy)]
    public async Task<ActionResult<MeDto>> Me(CancellationToken ct)
    {
        if (Sub is null) return Unauthorized();
        var me = await profiles.GetMeAsync(Sub, ct);
        return me is null ? Unauthorized() : Ok(me);
    }

    /// <summary>US-07 — atualiza nome/avatar próprios.</summary>
    [HttpPatch("v1/me")]
    [Authorize(Policy = AuthExtensions.UserPolicy)]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest body, CancellationToken ct)
    {
        if (Sub is null) return Unauthorized();
        return await profiles.UpdateAsync(Sub, body, ct) ? NoContent() : Unauthorized();
    }

    /// <summary>US-07 — RGPD: apagar a própria conta (soft-delete + anonimização).</summary>
    [HttpDelete("v1/me")]
    [Authorize(Policy = AuthExtensions.UserPolicy)]
    public async Task<IActionResult> Delete(CancellationToken ct)
    {
        if (Sub is null) return Unauthorized();
        return await profiles.DeleteAccountAsync(Sub, ct) ? NoContent() : Unauthorized();
    }
}
