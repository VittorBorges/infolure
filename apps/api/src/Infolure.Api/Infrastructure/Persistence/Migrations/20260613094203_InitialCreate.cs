using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infolure.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "species",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    family = table.Column<string>(type: "text", nullable: true),
                    water_type = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_species", x => x.id);
                    table.CheckConstraint("ck_species_water_type", "water_type IN ('freshwater','saltwater','both')");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    username = table.Column<string>(type: "text", nullable: true),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    locale = table.Column<string>(type: "text", nullable: false, defaultValue: "pt"),
                    role = table.Column<string>(type: "text", nullable: false, defaultValue: "user"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_user_role", "role IN ('user','admin')");
                });

            migrationBuilder.CreateTable(
                name: "brand_translations",
                columns: table => new
                {
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brand_translations", x => new { x.brand_id, x.locale });
                    table.ForeignKey(
                        name: "fk_brand_translations_brands_brand_id",
                        column: x => x.brand_id,
                        principalTable: "brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: true),
                    model_ref = table.Column<string>(type: "text", nullable: true),
                    lure_type = table.Column<string>(type: "text", nullable: false),
                    water_type = table.Column<string>(type: "text", nullable: true),
                    weight_g = table.Column<decimal>(type: "numeric", nullable: true),
                    length_mm = table.Column<decimal>(type: "numeric", nullable: true),
                    depth_min_m = table.Column<decimal>(type: "numeric", nullable: true),
                    depth_max_m = table.Column<decimal>(type: "numeric", nullable: true),
                    hook_size = table.Column<string>(type: "text", nullable: true),
                    hook_type = table.Column<string>(type: "text", nullable: true),
                    hook_count = table.Column<short>(type: "smallint", nullable: true),
                    material = table.Column<string>(type: "text", nullable: true),
                    attributes = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'"),
                    price6m_min_eur = table.Column<decimal>(type: "numeric", nullable: true),
                    price6m_max_eur = table.Column<decimal>(type: "numeric", nullable: true),
                    price6m_avg_eur = table.Column<decimal>(type: "numeric", nullable: true),
                    price6m_updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "draft"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lures", x => x.id);
                    table.CheckConstraint("ck_lure_status", "status IN ('draft','published','archived')");
                    table.CheckConstraint("ck_lure_water_type", "water_type IS NULL OR water_type IN ('freshwater','saltwater','both')");
                    table.ForeignKey(
                        name: "fk_lures_brands_brand_id",
                        column: x => x.brand_id,
                        principalTable: "brands",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "species_translations",
                columns: table => new
                {
                    species_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    common_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_species_translations", x => new { x.species_id, x.locale });
                    table.ForeignKey(
                        name: "fk_species_translations_species_species_id",
                        column: x => x.species_id,
                        principalTable: "species",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_auth_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    provider_uid = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    linked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_auth_providers", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_auth_providers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lure_colors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_pt = table.Column<string>(type: "text", nullable: false),
                    name_en = table.Column<string>(type: "text", nullable: true),
                    hex_primary = table.Column<string>(type: "text", nullable: true),
                    hex_secondary = table.Column<string>(type: "text", nullable: true),
                    pattern = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lure_colors", x => x.id);
                    table.ForeignKey(
                        name: "fk_lure_colors_lures_lure_id",
                        column: x => x.lure_id,
                        principalTable: "lures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lure_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    color_id = table.Column<Guid>(type: "uuid", nullable: true),
                    url = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lure_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_lure_images_lures_lure_id",
                        column: x => x.lure_id,
                        principalTable: "lures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lure_retailer_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retailer = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: true),
                    price_eur = table.Column<decimal>(type: "numeric", nullable: false),
                    in_stock = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lure_retailer_prices", x => x.id);
                    table.CheckConstraint("ck_retailer_price_positive", "price_eur > 0");
                    table.ForeignKey(
                        name: "fk_lure_retailer_prices_lures_lure_id",
                        column: x => x.lure_id,
                        principalTable: "lures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lure_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rating = table.Column<short>(type: "smallint", nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    locale = table.Column<string>(type: "text", nullable: false, defaultValue: "pt"),
                    helpful_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "published"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lure_reviews", x => x.id);
                    table.CheckConstraint("ck_review_body", "char_length(body) <= 1000");
                    table.CheckConstraint("ck_review_rating", "rating BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_review_status", "status IN ('pending','published','hidden')");
                    table.ForeignKey(
                        name: "fk_lure_reviews_lures_lure_id",
                        column: x => x.lure_id,
                        principalTable: "lures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lure_reviews_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "lure_target_species",
                columns: table => new
                {
                    lure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    species_id = table.Column<Guid>(type: "uuid", nullable: false),
                    confidence = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lure_target_species", x => new { x.lure_id, x.species_id });
                    table.CheckConstraint("ck_target_confidence", "confidence IN ('primary','secondary')");
                    table.ForeignKey(
                        name: "fk_lure_target_species_lures_lure_id",
                        column: x => x.lure_id,
                        principalTable: "lures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lure_target_species_species_species_id",
                        column: x => x.species_id,
                        principalTable: "species",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lure_translations",
                columns: table => new
                {
                    lure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lure_translations", x => new { x.lure_id, x.locale });
                    table.ForeignKey(
                        name: "fk_lure_translations_lures_lure_id",
                        column: x => x.lure_id,
                        principalTable: "lures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_lure_favorites",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_lure_favorites", x => new { x.user_id, x.lure_id });
                    table.ForeignKey(
                        name: "fk_user_lure_favorites_lures_lure_id",
                        column: x => x.lure_id,
                        principalTable: "lures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_lure_favorites_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_lure_inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    color_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    condition = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_lure_inventory", x => x.id);
                    table.CheckConstraint("ck_inventory_condition", "condition IS NULL OR condition IN ('new','good','used','lost')");
                    table.CheckConstraint("ck_inventory_notes", "char_length(notes) <= 200");
                    table.CheckConstraint("ck_inventory_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_user_lure_inventory_lures_lure_id",
                        column: x => x.lure_id,
                        principalTable: "lures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_lure_inventory_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_helpful_votes",
                columns: table => new
                {
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_helpful_votes", x => new { x.review_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_review_helpful_votes_lure_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "lure_reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_review_helpful_votes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_brands_slug",
                table: "brands",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lure_colors_lure_id",
                table: "lure_colors",
                column: "lure_id");

            migrationBuilder.CreateIndex(
                name: "ix_lure_images_lure_id",
                table: "lure_images",
                column: "lure_id");

            migrationBuilder.CreateIndex(
                name: "ix_lure_retailer_prices_lure_id",
                table: "lure_retailer_prices",
                column: "lure_id");

            migrationBuilder.CreateIndex(
                name: "ix_lure_reviews_lure_id_status",
                table: "lure_reviews",
                columns: new[] { "lure_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_lure_reviews_lure_id_user_id",
                table: "lure_reviews",
                columns: new[] { "lure_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lure_reviews_user_id",
                table: "lure_reviews",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_lure_target_species_species_id",
                table: "lure_target_species",
                column: "species_id");

            migrationBuilder.CreateIndex(
                name: "ix_lures_brand_id",
                table: "lures",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "ix_lures_lure_type",
                table: "lures",
                column: "lure_type");

            migrationBuilder.CreateIndex(
                name: "ix_lures_slug",
                table: "lures",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lures_status",
                table: "lures",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_lures_water_type",
                table: "lures",
                column: "water_type");

            migrationBuilder.CreateIndex(
                name: "ix_lures_weight_g",
                table: "lures",
                column: "weight_g");

            migrationBuilder.CreateIndex(
                name: "ix_review_helpful_votes_user_id",
                table: "review_helpful_votes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_species_slug",
                table: "species",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_auth_providers_provider_provider_uid",
                table: "user_auth_providers",
                columns: new[] { "provider", "provider_uid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_auth_providers_user_id",
                table: "user_auth_providers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_lure_favorites_lure_id",
                table: "user_lure_favorites",
                column: "lure_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_lure_favorites_user_id",
                table: "user_lure_favorites",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_lure_inventory_lure_id",
                table: "user_lure_inventory",
                column: "lure_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_lure_inventory_user_id",
                table: "user_lure_inventory",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_lure_inventory_user_id_lure_id_color_id",
                table: "user_lure_inventory",
                columns: new[] { "user_id", "lure_id", "color_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "brand_translations");

            migrationBuilder.DropTable(
                name: "lure_colors");

            migrationBuilder.DropTable(
                name: "lure_images");

            migrationBuilder.DropTable(
                name: "lure_retailer_prices");

            migrationBuilder.DropTable(
                name: "lure_target_species");

            migrationBuilder.DropTable(
                name: "lure_translations");

            migrationBuilder.DropTable(
                name: "review_helpful_votes");

            migrationBuilder.DropTable(
                name: "species_translations");

            migrationBuilder.DropTable(
                name: "user_auth_providers");

            migrationBuilder.DropTable(
                name: "user_lure_favorites");

            migrationBuilder.DropTable(
                name: "user_lure_inventory");

            migrationBuilder.DropTable(
                name: "lure_reviews");

            migrationBuilder.DropTable(
                name: "species");

            migrationBuilder.DropTable(
                name: "lures");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "brands");
        }
    }
}
