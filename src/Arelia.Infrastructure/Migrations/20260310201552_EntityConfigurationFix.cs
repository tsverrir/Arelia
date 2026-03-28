using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arelia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EntityConfigurationFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Activities_ParentActivityId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Persons_PersonId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleAssignments_Persons_PersonId",
                table: "RoleAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleAssignments_Roles_RoleId",
                table: "RoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_ActivityId",
                table: "AttendanceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_Permission",
                table: "RolePermissions",
                columns: new[] { "RoleId", "Permission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ActivityId_PersonId",
                table: "AttendanceRecords",
                columns: new[] { "ActivityId", "PersonId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Activities_ParentActivityId",
                table: "Activities",
                column: "ParentActivityId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Persons_PersonId",
                table: "AttendanceRecords",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleAssignments_Persons_PersonId",
                table: "RoleAssignments",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleAssignments_Roles_RoleId",
                table: "RoleAssignments",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Activities_ParentActivityId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Persons_PersonId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleAssignments_Persons_PersonId",
                table: "RoleAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleAssignments_Roles_RoleId",
                table: "RoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId_Permission",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_ActivityId_PersonId",
                table: "AttendanceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ActivityId",
                table: "AttendanceRecords",
                column: "ActivityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Activities_ParentActivityId",
                table: "Activities",
                column: "ParentActivityId",
                principalTable: "Activities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Persons_PersonId",
                table: "AttendanceRecords",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleAssignments_Persons_PersonId",
                table: "RoleAssignments",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleAssignments_Roles_RoleId",
                table: "RoleAssignments",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
