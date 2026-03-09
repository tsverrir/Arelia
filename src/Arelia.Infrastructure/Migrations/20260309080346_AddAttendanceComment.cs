using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arelia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "AttendanceRecords",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "AttendanceRecords");
        }
    }
}
