using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_users_ManagerUser_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_ManagerUser_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ManagerUser_id",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_users_Manager_id",
                table: "users",
                column: "Manager_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_Manager_id",
                table: "users",
                column: "Manager_id",
                principalTable: "users",
                principalColumn: "User_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_users_Manager_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_Manager_id",
                table: "users");

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerUser_id",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_users_ManagerUser_id",
                table: "users",
                column: "ManagerUser_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_ManagerUser_id",
                table: "users",
                column: "ManagerUser_id",
                principalTable: "users",
                principalColumn: "User_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
