using System.Text.RegularExpressions;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Features.Auth;

/// <summary>
/// Sincronização de utilizadores a partir do Supabase Auth (US-04).
/// Convenção: <c>provider_uid</c> guarda o id do utilizador no Supabase (= claim `sub` do JWT);
/// <c>provider</c> rotula o método (google/microsoft/email). Assim o mesmo utilizador pode ter
/// várias linhas (linking multi-provedor) e é localizável pelo `sub` do token.
/// </summary>
public partial class AuthSyncService(AppDbContext db)
{
    [GeneratedRegex(@"^[A-Za-z0-9_]{3,20}$")]
    private static partial Regex UsernameRegex();

    /// <summary>Cria ou atualiza o utilizador. Idempotente em (provider, provider_uid).</summary>
    public async Task<SyncUserResponse> SyncAsync(SyncUserRequest req, CancellationToken ct = default)
    {
        var link = await db.UserAuthProviders
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Provider == req.Provider && p.ProviderUid == req.ProviderUid, ct);

        User user;
        if (link is not null)
        {
            user = link.User;
            user.LastLoginAt = DateTimeOffset.UtcNow;
        }
        else
        {
            // Tenta reaproveitar o utilizador já existente para o mesmo sub do Supabase (outro provedor).
            user = await db.Users.FirstOrDefaultAsync(
                       u => db.UserAuthProviders.Any(p => p.UserId == u.Id && p.ProviderUid == req.ProviderUid), ct)
                   ?? new User
                   {
                       Id = Guid.NewGuid(),
                       Email = req.Email,
                       DisplayName = req.DisplayName,
                       AvatarUrl = req.AvatarUrl,
                       Locale = "pt",
                       Role = "user",
                       LastLoginAt = DateTimeOffset.UtcNow,
                   };

            if (db.Entry(user).State == EntityState.Detached)
                db.Users.Add(user);

            db.UserAuthProviders.Add(new UserAuthProvider
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = req.Provider,
                ProviderUid = req.ProviderUid,
                Email = req.Email,
            });
        }

        await db.SaveChangesAsync(ct);
        return new SyncUserResponse(user.Id, user.Username, user.Username is null);
    }

    /// <summary>Define o username (3–20, alfanumérico + underscore, único) — primeiro login OAuth.</summary>
    public async Task<(SetUsernameResult Result, string? Username)> SetUsernameAsync(
        string supabaseSub, string username, CancellationToken ct = default)
    {
        if (!UsernameRegex().IsMatch(username))
            return (SetUsernameResult.Invalid, null);

        var user = await db.Users.FirstOrDefaultAsync(
            u => db.UserAuthProviders.Any(p => p.UserId == u.Id && p.ProviderUid == supabaseSub), ct);
        if (user is null)
            return (SetUsernameResult.UserNotFound, null);

        var taken = await db.Users.AnyAsync(u => u.Username == username && u.Id != user.Id, ct);
        if (taken)
            return (SetUsernameResult.Taken, null);

        user.Username = username;
        await db.SaveChangesAsync(ct);
        return (SetUsernameResult.Ok, username);
    }
}
