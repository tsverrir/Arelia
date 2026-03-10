using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arelia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseChargeVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OriginalId",
                table: "Expenses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacedById",
                table: "Expenses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalId",
                table: "Charges",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacedById",
                table: "Charges",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_OriginalId",
                table: "Expenses",
                column: "OriginalId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ReplacedById",
                table: "Expenses",
                column: "ReplacedById",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Charges_OriginalId",
                table: "Charges",
                column: "OriginalId");

            migrationBuilder.CreateIndex(
                name: "IX_Charges_ReplacedById",
                table: "Charges",
                column: "ReplacedById",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Charges_Charges_OriginalId",
                table: "Charges",
                column: "OriginalId",
                principalTable: "Charges",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Charges_Charges_ReplacedById",
                table: "Charges",
                column: "ReplacedById",
                principalTable: "Charges",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Expenses_OriginalId",
                table: "Expenses",
                column: "OriginalId",
                principalTable: "Expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Expenses_ReplacedById",
                table: "Expenses",
                column: "ReplacedById",
                principalTable: "Expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Charges_Charges_OriginalId",
                table: "Charges");

            migrationBuilder.DropForeignKey(
                name: "FK_Charges_Charges_ReplacedById",
                table: "Charges");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Expenses_OriginalId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Expenses_ReplacedById",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_OriginalId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_ReplacedById",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Charges_OriginalId",
                table: "Charges");

            migrationBuilder.DropIndex(
                name: "IX_Charges_ReplacedById",
                table: "Charges");

            migrationBuilder.DropColumn(
                name: "OriginalId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "ReplacedById",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "OriginalId",
                table: "Charges");

            migrationBuilder.DropColumn(
                name: "ReplacedById",
                table: "Charges");
        }
    }
}
