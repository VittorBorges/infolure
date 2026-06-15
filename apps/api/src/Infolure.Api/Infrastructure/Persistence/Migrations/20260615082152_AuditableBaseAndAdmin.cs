using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infolure.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditableBaseAndAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "user_lure_inventory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "user_lure_inventory",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "user_lure_inventory",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "user_lure_favorites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "user_lure_favorites",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "user_lure_favorites",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "user_auth_providers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "user_auth_providers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "user_auth_providers",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "species_translations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "species_translations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "species_translations",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "species",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "species",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "species",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "review_helpful_votes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "review_helpful_votes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "review_helpful_votes",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "lures",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "lures",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_indexable",
                table: "lures",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "lures",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "lure_translations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "lure_translations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "lure_translations",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "lure_target_species",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "lure_target_species",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "lure_target_species",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "lure_reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "lure_reviews",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "lure_reviews",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "lure_retailer_prices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "lure_retailer_prices",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "lure_retailer_prices",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "lure_images",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "lure_images",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "lure_images",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "lure_colors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "lure_colors",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "lure_colors",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "brands",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "brands",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "brands",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "brand_translations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "brand_translations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "brand_translations",
                type: "text",
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.CreateTable(
                name: "admin_audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: false),
                    is_personal_data = table.Column<bool>(type: "boolean", nullable: false),
                    changes = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_audit_log", x => x.id);
                    table.CheckConstraint("ck_audit_action", "action IN ('create','update','activate','deactivate','delete','restore','moderate','settings_update')");
                });

            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    seo_indexing_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_settings", x => x.id);
                    table.CheckConstraint("ck_app_settings_singleton", "id = 1");
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_source",
                table: "users",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_lure_inventory_source",
                table: "user_lure_inventory",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_lure_favorites_source",
                table: "user_lure_favorites",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_auth_providers_source",
                table: "user_auth_providers",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_species_translations_source",
                table: "species_translations",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_species_source",
                table: "species",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_review_helpful_votes_source",
                table: "review_helpful_votes",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lures_source",
                table: "lures",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lure_translations_source",
                table: "lure_translations",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lure_target_species_source",
                table: "lure_target_species",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lure_reviews_source",
                table: "lure_reviews",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lure_retailer_prices_source",
                table: "lure_retailer_prices",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lure_images_source",
                table: "lure_images",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lure_colors_source",
                table: "lure_colors",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_brands_source",
                table: "brands",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_brand_translations_source",
                table: "brand_translations",
                sql: "source IN ('manual','automation','import')");

            migrationBuilder.CreateIndex(
                name: "ix_admin_audit_log_action_created_at",
                table: "admin_audit_log",
                columns: new[] { "action", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_admin_audit_log_actor_user_id_created_at",
                table: "admin_audit_log",
                columns: new[] { "actor_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_admin_audit_log_entity_type_entity_id",
                table: "admin_audit_log",
                columns: new[] { "entity_type", "entity_id" });

            // Feature 002 (T010) — backfill SC-008: os dados de catálogo pré-existentes vieram do
            // seed/scraping → origem 'automation'. Utilizadores e dados pessoais ficam 'manual'
            // (default). Linha singleton de app_settings (seo_indexing_enabled=true) para preservar
            // o comportamento de indexação da Feature 001.
            migrationBuilder.Sql(@"
                UPDATE brands SET source = 'automation';
                UPDATE brand_translations SET source = 'automation';
                UPDATE species SET source = 'automation';
                UPDATE species_translations SET source = 'automation';
                UPDATE lures SET source = 'automation';
                UPDATE lure_translations SET source = 'automation';
                UPDATE lure_colors SET source = 'automation';
                UPDATE lure_images SET source = 'automation';
                UPDATE lure_target_species SET source = 'automation';
                UPDATE lure_retailer_prices SET source = 'automation';
                INSERT INTO app_settings (id, seo_indexing_enabled, updated_at)
                  VALUES (1, true, now()) ON CONFLICT (id) DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_audit_log");

            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_source",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_lure_inventory_source",
                table: "user_lure_inventory");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_lure_favorites_source",
                table: "user_lure_favorites");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_auth_providers_source",
                table: "user_auth_providers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_species_translations_source",
                table: "species_translations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_species_source",
                table: "species");

            migrationBuilder.DropCheckConstraint(
                name: "ck_review_helpful_votes_source",
                table: "review_helpful_votes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lures_source",
                table: "lures");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lure_translations_source",
                table: "lure_translations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lure_target_species_source",
                table: "lure_target_species");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lure_reviews_source",
                table: "lure_reviews");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lure_retailer_prices_source",
                table: "lure_retailer_prices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lure_images_source",
                table: "lure_images");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lure_colors_source",
                table: "lure_colors");

            migrationBuilder.DropCheckConstraint(
                name: "ck_brands_source",
                table: "brands");

            migrationBuilder.DropCheckConstraint(
                name: "ck_brand_translations_source",
                table: "brand_translations");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "users");

            migrationBuilder.DropColumn(
                name: "source",
                table: "users");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "user_lure_inventory");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "user_lure_inventory");

            migrationBuilder.DropColumn(
                name: "source",
                table: "user_lure_inventory");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "user_lure_favorites");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "user_lure_favorites");

            migrationBuilder.DropColumn(
                name: "source",
                table: "user_lure_favorites");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "user_auth_providers");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "user_auth_providers");

            migrationBuilder.DropColumn(
                name: "source",
                table: "user_auth_providers");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "species_translations");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "species_translations");

            migrationBuilder.DropColumn(
                name: "source",
                table: "species_translations");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "species");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "species");

            migrationBuilder.DropColumn(
                name: "source",
                table: "species");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "review_helpful_votes");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "review_helpful_votes");

            migrationBuilder.DropColumn(
                name: "source",
                table: "review_helpful_votes");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "lures");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "lures");

            migrationBuilder.DropColumn(
                name: "is_indexable",
                table: "lures");

            migrationBuilder.DropColumn(
                name: "source",
                table: "lures");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "lure_translations");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "lure_translations");

            migrationBuilder.DropColumn(
                name: "source",
                table: "lure_translations");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "lure_target_species");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "lure_target_species");

            migrationBuilder.DropColumn(
                name: "source",
                table: "lure_target_species");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "lure_reviews");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "lure_reviews");

            migrationBuilder.DropColumn(
                name: "source",
                table: "lure_reviews");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "lure_retailer_prices");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "lure_retailer_prices");

            migrationBuilder.DropColumn(
                name: "source",
                table: "lure_retailer_prices");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "lure_images");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "lure_images");

            migrationBuilder.DropColumn(
                name: "source",
                table: "lure_images");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "lure_colors");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "lure_colors");

            migrationBuilder.DropColumn(
                name: "source",
                table: "lure_colors");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "source",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "brand_translations");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "brand_translations");

            migrationBuilder.DropColumn(
                name: "source",
                table: "brand_translations");
        }
    }
}
