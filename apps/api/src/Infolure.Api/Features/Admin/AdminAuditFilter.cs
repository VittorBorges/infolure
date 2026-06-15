using Infolure.Api.Features.Auth;
using Infolure.Api.Infrastructure.Persistence.Auditing;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Infolure.Api.Features.Admin;

/// <summary>
/// Ativa o <see cref="IAdminActionContext"/> para requisições sob /v1/admin, resolvendo o actor a
/// partir do `sub` do JWT. Assim, qualquer SaveChanges durante uma ação admin é auditado pelo
/// <see cref="AdminAuditInterceptor"/> (FR-020/SC-007), sem wiring por endpoint.
/// </summary>
public sealed class AdminAuditFilter(IAdminActionContext adminContext, UserResolver users) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var path = context.HttpContext.Request.Path;
        if (path.StartsWithSegments("/v1/admin"))
        {
            var sub = context.HttpContext.User.FindFirst("sub")?.Value;
            Guid? actor = sub is null ? null : await users.ResolveUserIdAsync(sub, context.HttpContext.RequestAborted);
            adminContext.Begin(actor);
        }
        await next();
    }
}
