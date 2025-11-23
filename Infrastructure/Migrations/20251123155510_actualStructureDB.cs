using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class actualStructureDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HierarchyId",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "users",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "hierarchies",
                columns: table => new
                {
                    HierarchyId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_id = table.Column<int>(type: "integer", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hierarchies", x => x.HierarchyId);
                    table.ForeignKey(
                        name: "FK_hierarchies_hierarchies_parent_id",
                        column: x => x.parent_id,
                        principalTable: "hierarchies",
                        principalColumn: "HierarchyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_HierarchyId",
                table: "users",
                column: "HierarchyId");

            migrationBuilder.CreateIndex(
                name: "IX_hierarchies_parent_id",
                table: "hierarchies",
                column: "parent_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_hierarchies_HierarchyId",
                table: "users",
                column: "HierarchyId",
                principalTable: "hierarchies",
                principalColumn: "HierarchyId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_hierarchies_HierarchyId",
                table: "users");

            migrationBuilder.DropTable(
                name: "hierarchies");

            migrationBuilder.DropIndex(
                name: "IX_users_HierarchyId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "HierarchyId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "users");
        }
    }
}
