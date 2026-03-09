using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arelia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVoiceGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoiceGroup",
                table: "Persons");

            migrationBuilder.AddColumn<Guid>(
                name: "VoiceGroupId",
                table: "Persons",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VoiceGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Persons_VoiceGroupId",
                table: "Persons",
                column: "VoiceGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceGroups_Name_OrganizationId",
                table: "VoiceGroups",
                columns: new[] { "Name", "OrganizationId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Persons_VoiceGroups_VoiceGroupId",
                table: "Persons",
                column: "VoiceGroupId",
                principalTable: "VoiceGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Persons_VoiceGroups_VoiceGroupId",
                table: "Persons");

            migrationBuilder.DropTable(
                name: "VoiceGroups");

            migrationBuilder.DropIndex(
                name: "IX_Persons_VoiceGroupId",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "VoiceGroupId",
                table: "Persons");

            migrationBuilder.AddColumn<int>(
                name: "VoiceGroup",
                table: "Persons",
                type: "INTEGER",
                nullable: true);
        }
    }
}
