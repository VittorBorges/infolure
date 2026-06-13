namespace Infolure.Api.Infrastructure.Persistence.Entities;

// Domínio de utilizador — espelha data-model.md.

public class User
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Locale { get; set; } = "pt";
    public string Role { get; set; } = "user"; // user | admin
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; } // RGPD soft-delete (forward-compat)

    public ICollection<UserAuthProvider> AuthProviders { get; set; } = new List<UserAuthProvider>();
    public ICollection<UserLureFavorite> Favorites { get; set; } = new List<UserLureFavorite>();
    public ICollection<UserLureInventory> Inventory { get; set; } = new List<UserLureInventory>();
}

public class UserAuthProvider
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = null!; // google | microsoft | email
    public string ProviderUid { get; set; } = null!;
    public string? Email { get; set; }
    public DateTimeOffset LinkedAt { get; set; }
    public User User { get; set; } = null!;
}

public class UserLureFavorite
{
    public Guid UserId { get; set; }
    public Guid LureId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public User User { get; set; } = null!;
    public Lure Lure { get; set; } = null!;
}

public class UserLureInventory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LureId { get; set; }
    public Guid? ColorId { get; set; }
    public short Quantity { get; set; } = 1;
    public string? Condition { get; set; } // new | good | used | lost
    public string? Notes { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    public User User { get; set; } = null!;
    public Lure Lure { get; set; } = null!;
}
