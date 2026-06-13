using Infolure.Api.Features.Auth;
using Infolure.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Features.Users;

public record PublicProfileDto(
    string Username,
    string? AvatarUrl,
    DateTimeOffset MemberSince,
    int FavoritesCount,
    int InventoryCount,
    int ReviewsCount);

public record UpdateProfileRequest(string? DisplayName, string? AvatarUrl);

/// <summary>
/// Perfil público e gestão da própria conta (US-07). O perfil público NÃO expõe PII
/// (email, nome real) — apenas username, avatar, data de adesão e contagens.
/// </summary>
public class ProfileService(AppDbContext db, UserResolver users, ILogger<ProfileService> logger)
{
    public async Task<PublicProfileDto?> GetPublicProfileAsync(string username, CancellationToken ct = default)
    {
        var user = await db.Users
            .Where(u => u.Username == username && u.DeletedAt == null)
            .Select(u => new { u.Id, u.Username, u.AvatarUrl, u.CreatedAt })
            .FirstOrDefaultAsync(ct);
        if (user is null) return null;

        var favorites = await db.UserLureFavorites.CountAsync(f => f.UserId == user.Id, ct);
        var inventory = await db.UserLureInventory.Select(i => i.UserId).CountAsync(id => id == user.Id, ct);
        var reviews = await db.LureReviews.CountAsync(r => r.UserId == user.Id && r.Status == "published", ct);

        return new PublicProfileDto(user.Username!, user.AvatarUrl, user.CreatedAt, favorites, inventory, reviews);
    }

    public async Task<bool> UpdateAsync(string sub, UpdateProfileRequest req, CancellationToken ct = default)
    {
        var userId = await users.ResolveUserIdAsync(sub, ct);
        if (userId is null) return false;
        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);
        if (req.DisplayName is not null) user.DisplayName = req.DisplayName;
        if (req.AvatarUrl is not null) user.AvatarUrl = req.AvatarUrl;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>RGPD: soft-delete (deleted_at), anonimiza PII e (stub) envia email de confirmação.</summary>
    public async Task<bool> DeleteAccountAsync(string sub, CancellationToken ct = default)
    {
        var userId = await users.ResolveUserIdAsync(sub, ct);
        if (userId is null) return false;
        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);

        var email = user.Email;
        user.DeletedAt = DateTimeOffset.UtcNow;
        user.Email = null;
        user.DisplayName = null;
        user.AvatarUrl = null;
        // Remove os vínculos de provedores (revoga acesso); reviews ficam anónimas via FK SET NULL.
        db.UserAuthProviders.RemoveRange(db.UserAuthProviders.Where(p => p.UserId == userId));
        await db.SaveChangesAsync(ct);

        // TODO(email): integrar serviço de email. Sem SMTP configurado, apenas regista a intenção.
        logger.LogInformation("RGPD: conta {UserId} apagada; email de confirmação a enviar para {HasEmail}.",
            userId, email is not null);
        return true;
    }
}
