using System.Security.Claims;
using Infolure.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Infolure.Api.Infrastructure.Auth;

/// <summary>
/// Feature 002 (T011 / FR-013a): após a autenticação JWT, garante que o utilizador local
/// está ativo e não eliminado. Caso contrário → 401 (sessão deixa de ser aceite de imediato).
/// Também injeta a role da BD como claim "role", tornando a BD a fonte de verdade da AdminPolicy.
/// O estado é cacheado em Redis (TTL curto) quando disponível; invalidado pelo painel ao
/// desativar/eliminar (US-02).
/// </summary>
public sealed class ActiveUserMiddleware(RequestDelegate next)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);
    private const string Blocked = "\0blocked"; // sentinela cacheável para utilizador inativo/eliminado

    public async Task InvokeAsync(HttpContext ctx, AppDbContext db)
    {
        var sub = ctx.User.FindFirst("sub")?.Value
                  ?? ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(sub))
        {
            await next(ctx); // anónimo — autorização por endpoint trata o resto
            return;
        }

        var redis = ctx.RequestServices.GetService<IConnectionMultiplexer>();
        var cacheKey = $"user:state:{sub}";
        IDatabase? cache = null;
        string? state = null;

        if (redis is not null)
        {
            cache = redis.GetDatabase();
            state = await cache.StringGetAsync(cacheKey);
        }

        if (state is null)
        {
            // IgnoreQueryFilters: precisamos de VER o utilizador mesmo soft-deleted, para o bloquear.
            var u = await db.Users.IgnoreQueryFilters()
                .Where(x => x.AuthProviders.Any(p => p.ProviderUid == sub))
                .Select(x => new { x.IsActive, x.DeletedAt, x.Role })
                .FirstOrDefaultAsync(ctx.RequestAborted);

            // Utilizador ainda não sincronizado localmente → não bloquear (auth-sync trata).
            state = u is null ? string.Empty
                  : (!u.IsActive || u.DeletedAt is not null) ? Blocked
                  : u.Role;

            if (cache is not null)
                await cache.StringSetAsync(cacheKey, state, CacheTtl);
        }

        if (state == Blocked)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Injeta a role da BD como claim (fonte de verdade da AdminPolicy).
        if (!string.IsNullOrEmpty(state) && ctx.User.Identity is ClaimsIdentity id)
        {
            foreach (var existing in id.FindAll("role").ToList()) id.RemoveClaim(existing);
            id.AddClaim(new Claim("role", state));
        }

        await next(ctx);
    }
}
