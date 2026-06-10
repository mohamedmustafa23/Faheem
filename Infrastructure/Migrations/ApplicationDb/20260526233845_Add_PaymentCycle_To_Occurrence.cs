using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class Add_PaymentCycle_To_Occurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentCycleId",
                schema: "Academics",
                table: "SessionOccurrences",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionOccurrences_PaymentCycleId",
                schema: "Academics",
                table: "SessionOccurrences",
                column: "PaymentCycleId",
                filter: "[PaymentCycleId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionOccurrences_PaymentCycles_PaymentCycleId",
                schema: "Academics",
                table: "SessionOccurrences",
                column: "PaymentCycleId",
                principalSchema: "Academics",
                principalTable: "PaymentCycles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionOccurrences_PaymentCycles_PaymentCycleId",
                schema: "Academics",
                table: "SessionOccurrences");

            migrationBuilder.DropIndex(
                name: "IX_SessionOccurrences_PaymentCycleId",
                schema: "Academics",
                table: "SessionOccurrences");

            migrationBuilder.DropColumn(
                name: "PaymentCycleId",
                schema: "Academics",
                table: "SessionOccurrences");
        }
    }
}
