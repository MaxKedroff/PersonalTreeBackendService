using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardResetMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    User_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    HierarchyId = table.Column<int>(type: "integer", nullable: true),
                    Manager_id = table.Column<Guid>(type: "uuid", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    patronymic = table.Column<string>(type: "text", nullable: false),
                    birth_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    interests = table.Column<string>(type: "text", nullable: false),
                    position = table.Column<string>(type: "text", nullable: false),
                    department = table.Column<string>(type: "text", nullable: false),
                    work_exp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    city = table.Column<string>(type: "text", nullable: false),
                    avatar = table.Column<string>(type: "text", nullable: false),
                    new_avatar = table.Column<string>(type: "text", nullable: false),
                    Contacts = table.Column<string>(type: "jsonb", nullable: false),
                    Skills = table.Column<string[]>(type: "text[]", nullable: false),
                    SamAccountName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastAdSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AdGuid = table.Column<string>(type: "text", nullable: false),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.User_id);
                    table.ForeignKey(
                        name: "FK_users_hierarchies_HierarchyId",
                        column: x => x.HierarchyId,
                        principalTable: "hierarchies",
                        principalColumn: "HierarchyId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_users_users_Manager_id",
                        column: x => x.Manager_id,
                        principalTable: "users",
                        principalColumn: "User_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_hierarchies_parent_id",
                table: "hierarchies",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_HierarchyId",
                table: "users",
                column: "HierarchyId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Manager_id",
                table: "users",
                column: "Manager_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "hierarchies");
        }
    }
}
