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

    /// <summary>RGPD (self-service): erasure efetiva da própria conta.</summary>
    public async Task<bool> DeleteAccountAsync(string sub, CancellationToken ct = default)
    {
        var userId = await users.ResolveUserIdAsync(sub, ct);
        if (userId is null) return false;
        return await EraseUserAsync(userId.Value, "self", ct);
    }

    /// <summary>
    /// RGPD: erasure efetiva e IRREVERSÍVEL de um utilizador (FR-012a) — anonimiza PII
    /// (email/nome/avatar), remove os vínculos de autenticação (revoga acesso) e marca eliminado.
    /// Distinta do soft-delete reversível do painel. Reutilizada pelo self-service e pelo admin.
    /// </summary>
    public async Task<bool> EraseUserAsync(Guid userId, string initiatedBy, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return false;

        var hadEmail = user.Email is not null;
        user.DeletedAt ??= DateTimeOffset.UtcNow;
        user.IsActive = false;
        user.Email = null;
        user.DisplayName = null;
        user.AvatarUrl = null;
        // Remove os vínculos de provedores (revoga acesso); reviews ficam anónimas via FK SET NULL.
        db.UserAuthProviders.RemoveRange(db.UserAuthProviders.Where(p => p.UserId == userId));
        await db.SaveChangesAsync(ct);

        // TODO(email): integrar serviço de email. Sem SMTP configurado, apenas regista a intenção.
        logger.LogInformation("RGPD: conta {UserId} anonimizada ({InitiatedBy}); confirmação por email: {HasEmail}.",
            userId, initiatedBy, hadEmail);
        return true;
    }
}
