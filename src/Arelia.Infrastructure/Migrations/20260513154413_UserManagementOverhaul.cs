using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arelia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserManagementOverhaul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationUsers_Persons_PersonId",
                table: "OrganizationUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "OrganizationUsers");

            migrationBuilder.AddColumn<int>(
                name: "RoleType",
                table: "Roles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 3); // Default to Custom (3)

            // Backfill RoleType based on role name for seeded roles
            // Admin=0, Board=1, Member=2, Custom=3
            migrationBuilder.Sql("UPDATE Roles SET RoleType = 0 WHERE Name = 'Admin'");
            migrationBuilder.Sql("UPDATE Roles SET RoleType = 1 WHERE Name = 'Board'");
            migrationBuilder.Sql("UPDATE Roles SET RoleType = 2 WHERE Name = 'Member'");
            // Treasurer and Conductor remain Custom (3)

            // Remove any OrganizationUser rows with null PersonId to satisfy the NOT NULL constraint
            migrationBuilder.Sql("DELETE FROM OrganizationUsers WHERE PersonId IS NULL OR PersonId = '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.AlterColumn<Guid>(
                name: "PersonId",
                table: "OrganizationUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationUsers_Persons_PersonId",
                table: "OrganizationUsers",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationUsers_Persons_PersonId",
                table: "OrganizationUsers");

            migrationBuilder.DropColumn(
                name: "RoleType",
                table: "Roles");

            migrationBuilder.AlterColumn<Guid>(
                name: "PersonId",
                table: "OrganizationUsers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "OrganizationUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationUsers_Persons_PersonId",
                table: "OrganizationUsers",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
