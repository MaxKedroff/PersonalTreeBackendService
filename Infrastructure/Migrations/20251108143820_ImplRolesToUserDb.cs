using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ImplRolesToUserDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Is_admin",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "users");

            migrationBuilder.AddColumn<bool>(
                name: "Is_admin",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
