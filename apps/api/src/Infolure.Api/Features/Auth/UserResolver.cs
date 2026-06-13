using Infolure.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Features.Auth;

/// <summary>
/// Resolve o utilizador local a partir do claim `sub` do JWT do Supabase
/// (= provider_uid nas linhas de user_auth_providers). Partilhado por favoritos,
/// inventário, reviews, perfil.
/// </summary>
public class UserResolver(AppDbContext db)
{
    public Task<Guid?> ResolveUserIdAsync(string sub, CancellationToken ct = default)
        => db.UserAuthProviders
            .Where(p => p.ProviderUid == sub)
            .Select(p => (Guid?)p.UserId)
            .FirstOrDefaultAsync(ct);
}
