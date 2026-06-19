using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infolure.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LureConfigurationsHooksAndGlobalIndexing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Feature 006 — RENAME (preserva dados) de lure_sizes → lure_configurations, com os
            // respetivos PK/FK/índice/check; + colunas de anzol por configuração.
            migrationBuilder.RenameTable(name: "lure_sizes", newName: "lure_configurations");
            migrationBuilder.RenameIndex(
                name: "ix_lure_sizes_lure_id", newName: "ix_lure_configurations_lure_id",
                table: "lure_configurations");
            migrationBuilder.Sql("ALTER TABLE lure_configurations RENAME CONSTRAINT pk_lure_sizes TO pk_lure_configurations;");
            migrationBuilder.Sql("ALTER TABLE lure_configurations RENAME CONSTRAINT fk_lure_sizes_lures_lure_id TO fk_lure_configurations_lures_lure_id;");
            migrationBuilder.Sql("ALTER TABLE lure_configurations RENAME CONSTRAINT ck_lure_sizes_source TO ck_lure_configurations_source;");

            migrationBuilder.AddColumn<string>(name: "hook_size", table: "lure_configurations", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "hook_type", table: "lure_configurations", type: "text", nullable: true);
            migrationBuilder.AddColumn<short>(name: "hook_count", table: "lure_configurations", type: "smallint", nullable: true);

            // Anzol e indexação por isca deixam de existir ao nível de lures (Feature 006).
            migrationBuilder.DropColumn(name: "hook_count", table: "lures");
            migrationBuilder.DropColumn(name: "hook_size", table: "lures");
            migrationBuilder.DropColumn(name: "hook_type", table: "lures");
            migrationBuilder.DropColumn(name: "is_indexable", table: "lures");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(name: "hook_count", table: "lures", type: "smallint", nullable: true);
            migrationBuilder.AddColumn<string>(name: "hook_size", table: "lures", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "hook_type", table: "lures", type: "text", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "is_indexable", table: "lures", type: "boolean", nullable: false, defaultValue: true);

            migrationBuilder.DropColumn(name: "hook_size", table: "lure_configurations");
            migrationBuilder.DropColumn(name: "hook_type", table: "lure_configurations");
            migrationBuilder.DropColumn(name: "hook_count", table: "lure_configurations");

            migrationBuilder.Sql("ALTER TABLE lure_configurations RENAME CONSTRAINT pk_lure_configurations TO pk_lure_sizes;");
            migrationBuilder.Sql("ALTER TABLE lure_configurations RENAME CONSTRAINT fk_lure_configurations_lures_lure_id TO fk_lure_sizes_lures_lure_id;");
            migrationBuilder.Sql("ALTER TABLE lure_configurations RENAME CONSTRAINT ck_lure_configurations_source TO ck_lure_sizes_source;");
            migrationBuilder.RenameIndex(
                name: "ix_lure_configurations_lure_id", newName: "ix_lure_sizes_lure_id",
                table: "lure_sizes");
            migrationBuilder.RenameTable(name: "lure_configurations", newName: "lure_sizes");
        }
    }
}
