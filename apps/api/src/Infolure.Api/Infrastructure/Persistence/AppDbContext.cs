using System.Linq.Expressions;
using Infolure.Api.Infrastructure.Persistence.Auditing;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, IAdminActionContext? adminContext = null)
    : DbContext(options)
{
    /// <summary>Contexto de ação admin (scoped); null fora de requisições admin. Lido pelo AdminAuditInterceptor.</summary>
    public IAdminActionContext? AdminContext { get; } = adminContext;

    // Catalog
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<BrandTranslation> BrandTranslations => Set<BrandTranslation>();
    public DbSet<Species> Species => Set<Species>();
    public DbSet<SpeciesTranslation> SpeciesTranslations => Set<SpeciesTranslation>();
    public DbSet<Lure> Lures => Set<Lure>();
    public DbSet<LureTranslation> LureTranslations => Set<LureTranslation>();
    public DbSet<LureConfiguration> LureConfigurations => Set<LureConfiguration>();
    public DbSet<LureColor> LureColors => Set<LureColor>();
    public DbSet<LureImage> LureImages => Set<LureImage>();
    public DbSet<LureTargetSpecies> LureTargetSpecies => Set<LureTargetSpecies>();
    public DbSet<LureRetailerPrice> LureRetailerPrices => Set<LureRetailerPrice>();

    // Users
    public DbSet<User> Users => Set<User>();
    public DbSet<UserAuthProvider> UserAuthProviders => Set<UserAuthProvider>();
    public DbSet<UserLureFavorite> UserLureFavorites => Set<UserLureFavorite>();
    public DbSet<UserLureInventory> UserLureInventory => Set<UserLureInventory>();

    // Content
    public DbSet<LureReview> LureReviews => Set<LureReview>();
    public DbSet<ReviewHelpfulVote> ReviewHelpfulVotes => Set<ReviewHelpfulVote>();

    // Admin / configuração (Feature 002)
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<AdminAuditEntry> AdminAuditLog => Set<AdminAuditEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ---- Catalog ----
        b.Entity<Brand>(e => e.HasIndex(x => x.Slug).IsUnique());

        b.Entity<BrandTranslation>(e =>
        {
            e.HasKey(x => new { x.BrandId, x.Locale });
            e.HasOne(x => x.Brand).WithMany(x => x.Translations)
                .HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Species>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("ck_species_water_type",
                "water_type IN ('freshwater','saltwater','both')"));
        });

        b.Entity<SpeciesTranslation>(e =>
        {
            e.HasKey(x => new { x.SpeciesId, x.Locale });
            e.HasOne(x => x.Species).WithMany(x => x.Translations)
                .HasForeignKey(x => x.SpeciesId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Lure>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.LureType);
            e.HasIndex(x => x.WaterType);
            e.HasIndex(x => x.BrandId);
            e.HasIndex(x => x.Status);
            e.Property(x => x.Attributes).HasColumnType("jsonb").HasDefaultValueSql("'{}'");
            e.Property(x => x.Status).HasDefaultValue("draft");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_lure_water_type",
                    "water_type IS NULL OR water_type IN ('freshwater','saltwater','both')");
                t.HasCheckConstraint("ck_lure_status",
                    "status IN ('draft','published','archived')");
            });
            e.HasOne(x => x.Brand).WithMany(x => x.Lures).HasForeignKey(x => x.BrandId);
        });

        b.Entity<LureTranslation>(e =>
        {
            e.HasKey(x => new { x.LureId, x.Locale });
            e.HasOne(x => x.Lure).WithMany(x => x.Translations)
                .HasForeignKey(x => x.LureId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<LureConfiguration>(e =>
        {
            e.HasIndex(x => x.LureId);
            e.HasOne(x => x.Lure).WithMany(x => x.Configurations)
                .HasForeignKey(x => x.LureId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<LureColor>(e =>
        {
            // Feature 005 — hex_codes como coluna jsonb (owned-collection .ToJson()).
            e.OwnsMany(x => x.HexCodes, nb => nb.ToJson("hex_codes"));
            e.HasOne(x => x.Lure).WithMany(x => x.Colors)
                .HasForeignKey(x => x.LureId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<LureImage>(e =>
        {
            e.Property(x => x.IsPrimary).HasDefaultValue(false);
            e.HasOne(x => x.Lure).WithMany(x => x.Images)
                .HasForeignKey(x => x.LureId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<LureTargetSpecies>(e =>
        {
            e.HasKey(x => new { x.LureId, x.SpeciesId });
            e.ToTable(t => t.HasCheckConstraint("ck_target_confidence",
                "confidence IN ('primary','secondary')"));
            e.HasOne(x => x.Lure).WithMany(x => x.TargetSpecies)
                .HasForeignKey(x => x.LureId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Species).WithMany()
                .HasForeignKey(x => x.SpeciesId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<LureRetailerPrice>(e =>
        {
            e.Property(x => x.InStock).HasDefaultValue(true);
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.ToTable(t => t.HasCheckConstraint("ck_retailer_price_positive", "price_eur > 0"));
            e.HasOne(x => x.Lure).WithMany(x => x.RetailerPrices)
                .HasForeignKey(x => x.LureId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---- Users ----
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Locale).HasDefaultValue("pt");
            e.Property(x => x.Role).HasDefaultValue("user");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.ToTable(t => t.HasCheckConstraint("ck_user_role", "role IN ('user','admin')"));
        });

        b.Entity<UserAuthProvider>(e =>
        {
            e.HasIndex(x => new { x.Provider, x.ProviderUid }).IsUnique();
            e.Property(x => x.LinkedAt).HasDefaultValueSql("now()");
            e.HasOne(x => x.User).WithMany(x => x.AuthProviders)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<UserLureFavorite>(e =>
        {
            e.HasKey(x => new { x.UserId, x.LureId });
            e.HasIndex(x => x.UserId);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(x => x.User).WithMany(x => x.Favorites)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Lure).WithMany()
                .HasForeignKey(x => x.LureId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<UserLureInventory>(e =>
        {
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => new { x.UserId, x.LureId, x.ColorId }).IsUnique();
            e.Property(x => x.Quantity).HasDefaultValue((short)1);
            e.Property(x => x.AddedAt).HasDefaultValueSql("now()");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_inventory_quantity", "quantity > 0");
                t.HasCheckConstraint("ck_inventory_condition",
                    "condition IS NULL OR condition IN ('new','good','used','lost')");
                t.HasCheckConstraint("ck_inventory_notes", "char_length(notes) <= 200");
            });
            e.HasOne(x => x.User).WithMany(x => x.Inventory)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Lure).WithMany()
                .HasForeignKey(x => x.LureId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---- Content ----
        b.Entity<LureReview>(e =>
        {
            e.HasIndex(x => new { x.LureId, x.UserId }).IsUnique();
            e.HasIndex(x => new { x.LureId, x.Status });
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Locale).HasDefaultValue("pt");
            e.Property(x => x.HelpfulCount).HasDefaultValue(0);
            e.Property(x => x.Status).HasDefaultValue("published");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_review_rating", "rating BETWEEN 1 AND 5");
                t.HasCheckConstraint("ck_review_body", "char_length(body) <= 1000");
                t.HasCheckConstraint("ck_review_status", "status IN ('pending','published','hidden')");
            });
            e.HasOne(x => x.Lure).WithMany()
                .HasForeignKey(x => x.LureId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<ReviewHelpfulVote>(e =>
        {
            e.HasKey(x => new { x.ReviewId, x.UserId });
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(x => x.Review).WithMany(x => x.HelpfulVotes)
                .HasForeignKey(x => x.ReviewId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---- Admin / configuração (Feature 002) ----
        b.Entity<AppSetting>(e =>
        {
            e.ToTable("app_settings", t => t.HasCheckConstraint("ck_app_settings_singleton", "id = 1"));
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.SeoIndexingEnabled).HasDefaultValue(true);
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        });

        b.Entity<AdminAuditEntry>(e =>
        {
            e.ToTable("admin_audit_log", t => t.HasCheckConstraint("ck_audit_action",
                "action IN ('create','update','activate','deactivate','delete','restore','moderate','settings_update')"));
            e.Property(x => x.Changes).HasColumnType("jsonb");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.HasIndex(x => new { x.ActorUserId, x.CreatedAt });
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => new { x.Action, x.CreatedAt });
        });

        // ---- Base auditável por convenção (Feature 002, T007) ----
        // Para toda entidade IAuditable: defaults de IsActive/Source, check de Source,
        // e global query filter (DeletedAt == null) que oculta soft-deletes de todas as queries.
        foreach (var et in b.Model.GetEntityTypes())
        {
            if (!typeof(IAuditable).IsAssignableFrom(et.ClrType)) continue;

            var entity = b.Entity(et.ClrType);
            entity.Property(nameof(IAuditable.IsActive)).HasDefaultValue(true);
            entity.Property(nameof(IAuditable.Source)).HasDefaultValue(AuditSource.Manual);

            var table = et.GetTableName();
            if (table is not null)
                entity.ToTable(t => t.HasCheckConstraint(
                    $"ck_{table}_source", "source IN ('manual','automation','import')"));

            // e => e.DeletedAt == null
            var p = Expression.Parameter(et.ClrType, "e");
            var body = Expression.Equal(
                Expression.Property(p, nameof(IAuditable.DeletedAt)),
                Expression.Constant(null, typeof(DateTimeOffset?)));
            entity.HasQueryFilter(Expression.Lambda(body, p));
        }
    }
}
