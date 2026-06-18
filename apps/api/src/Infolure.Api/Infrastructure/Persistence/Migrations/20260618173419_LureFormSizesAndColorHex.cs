using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infolure.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LureFormSizesAndColorHex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_lures_weight_g",
                table: "lures");

            migrationBuilder.DropColumn(
                name: "length_mm",
                table: "lures");

            migrationBuilder.DropColumn(
                name: "weight_g",
                table: "lures");

            migrationBuilder.DropColumn(
                name: "hex_primary",
                table: "lure_colors");

            migrationBuilder.DropColumn(
                name: "hex_secondary",
                table: "lure_colors");

            migrationBuilder.AddColumn<string>(
                name: "hex_codes",
                table: "lure_colors",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "lure_sizes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: true),
                    label = table.Column<string>(type: "text", nullable: false),
                    length_mm = table.Column<decimal>(type: "numeric", nullable: true),
                    weight_g = table.Column<decimal>(type: "numeric", nullable: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    source = table.Column<string>(type: "text", nullable: false, defaultValue: "manual"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lure_sizes", x => x.id);
                    table.CheckConstraint("ck_lure_sizes_source", "source IN ('manual','automation','import')");
                    table.ForeignKey(
                        name: "fk_lure_sizes_lures_lure_id",
                        column: x => x.lure_id,
                        principalTable: "lures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lure_sizes_lure_id",
                table: "lure_sizes",
                column: "lure_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lure_sizes");

            migrationBuilder.DropColumn(
                name: "hex_codes",
                table: "lure_colors");

            migrationBuilder.AddColumn<decimal>(
                name: "length_mm",
                table: "lures",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "weight_g",
                table: "lures",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hex_primary",
                table: "lure_colors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hex_secondary",
                table: "lure_colors",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_lures_weight_g",
                table: "lures",
                column: "weight_g");
        }
    }
}
