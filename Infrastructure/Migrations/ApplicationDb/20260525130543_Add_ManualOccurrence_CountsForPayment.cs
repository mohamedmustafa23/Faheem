using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class Add_ManualOccurrence_CountsForPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionOccurrences_SessionId_OccurrenceDate",
                schema: "Academics",
                table: "SessionOccurrences");

            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                schema: "Academics",
                table: "SessionOccurrences",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<bool>(
                name: "CountsForPayment",
                schema: "Academics",
                table: "SessionOccurrences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTime",
                schema: "Academics",
                table: "SessionOccurrences",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTime",
                schema: "Academics",
                table: "SessionOccurrences",
                type: "time",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionOccurrences_SessionId_OccurrenceDate",
                schema: "Academics",
                table: "SessionOccurrences",
                columns: new[] { "SessionId", "OccurrenceDate" },
                unique: true,
                filter: "[SessionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionOccurrences_SessionId_OccurrenceDate",
                schema: "Academics",
                table: "SessionOccurrences");

            migrationBuilder.DropColumn(
                name: "CountsForPayment",
                schema: "Academics",
                table: "SessionOccurrences");

            migrationBuilder.DropColumn(
                name: "EndTime",
                schema: "Academics",
                table: "SessionOccurrences");

            migrationBuilder.DropColumn(
                name: "StartTime",
                schema: "Academics",
                table: "SessionOccurrences");

            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                schema: "Academics",
                table: "SessionOccurrences",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionOccurrences_SessionId_OccurrenceDate",
                schema: "Academics",
                table: "SessionOccurrences",
                columns: new[] { "SessionId", "OccurrenceDate" },
                unique: true);
        }
    }
}
