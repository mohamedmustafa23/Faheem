using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class Add_Concurrency_And_Constraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "Academics",
                table: "StudentPaymentRecords",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "Academics",
                table: "PaymentCycles",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_StudentPaymentRecords_GroupId_Status",
                schema: "Academics",
                table: "StudentPaymentRecords",
                columns: new[] { "GroupId", "Status" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StudentPaymentRecord_CycleXorOccurrence",
                schema: "Academics",
                table: "StudentPaymentRecords",
                sql: "([PaymentCycleId] IS NOT NULL AND [OccurrenceId] IS NULL) OR ([PaymentCycleId] IS NULL AND [OccurrenceId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCycles_GroupId_IsCompleted",
                schema: "Academics",
                table: "PaymentCycles",
                columns: new[] { "GroupId", "IsCompleted" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentCycle_SessionsCompleted_NonNegative",
                schema: "Academics",
                table: "PaymentCycles",
                sql: "[SessionsCompleted] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentPaymentRecords_GroupId_Status",
                schema: "Academics",
                table: "StudentPaymentRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StudentPaymentRecord_CycleXorOccurrence",
                schema: "Academics",
                table: "StudentPaymentRecords");

            migrationBuilder.DropIndex(
                name: "IX_PaymentCycles_GroupId_IsCompleted",
                schema: "Academics",
                table: "PaymentCycles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentCycle_SessionsCompleted_NonNegative",
                schema: "Academics",
                table: "PaymentCycles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "Academics",
                table: "StudentPaymentRecords");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "Academics",
                table: "PaymentCycles");
        }
    }
}
