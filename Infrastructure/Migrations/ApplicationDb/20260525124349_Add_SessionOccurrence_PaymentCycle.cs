using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class Add_SessionOccurrence_PaymentCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Sessions_SessionId",
                schema: "Academics",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentPayments_Groups_GroupId",
                schema: "Academics",
                table: "StudentPayments");

            migrationBuilder.DropIndex(
                name: "IX_StudentPayments_GroupId_StudentId_Month_Year",
                schema: "Academics",
                table: "StudentPayments");

            migrationBuilder.DropColumn(
                name: "Month",
                schema: "Academics",
                table: "StudentPayments");

            migrationBuilder.DropColumn(
                name: "Year",
                schema: "Academics",
                table: "StudentPayments");

            migrationBuilder.DropColumn(
                name: "QrToken",
                schema: "Academics",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SpecificDate",
                schema: "Academics",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "Academics",
                table: "Sessions");

            migrationBuilder.RenameColumn(
                name: "IsRecurring",
                schema: "Academics",
                table: "Sessions",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "MonthlySessionCount",
                schema: "Academics",
                table: "Groups",
                newName: "SessionsPerCycle");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                schema: "Academics",
                table: "AttendanceRecords",
                newName: "OccurrenceId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecords_SessionId_StudentId",
                schema: "Academics",
                table: "AttendanceRecords",
                newName: "IX_AttendanceRecords_OccurrenceId_StudentId");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentCycleId",
                schema: "Academics",
                table: "StudentPayments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "PaymentCycles",
                schema: "Academics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleNumber = table.Column<int>(type: "int", nullable: false),
                    SessionsTarget = table.Column<int>(type: "int", nullable: false),
                    SessionsCompleted = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentCycles_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "Academics",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionOccurrences",
                schema: "Academics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QrToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionOccurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionOccurrences_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "Academics",
                        principalTable: "Groups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SessionOccurrences_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Academics",
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentPayments_PaymentCycleId_StudentId",
                schema: "Academics",
                table: "StudentPayments",
                columns: new[] { "PaymentCycleId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCycles_GroupId_CycleNumber",
                schema: "Academics",
                table: "PaymentCycles",
                columns: new[] { "GroupId", "CycleNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionOccurrences_GroupId",
                schema: "Academics",
                table: "SessionOccurrences",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionOccurrences_SessionId_OccurrenceDate",
                schema: "Academics",
                table: "SessionOccurrences",
                columns: new[] { "SessionId", "OccurrenceDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_SessionOccurrences_OccurrenceId",
                schema: "Academics",
                table: "AttendanceRecords",
                column: "OccurrenceId",
                principalSchema: "Academics",
                principalTable: "SessionOccurrences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPayments_PaymentCycles_PaymentCycleId",
                schema: "Academics",
                table: "StudentPayments",
                column: "PaymentCycleId",
                principalSchema: "Academics",
                principalTable: "PaymentCycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_SessionOccurrences_OccurrenceId",
                schema: "Academics",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentPayments_PaymentCycles_PaymentCycleId",
                schema: "Academics",
                table: "StudentPayments");

            migrationBuilder.DropTable(
                name: "PaymentCycles",
                schema: "Academics");

            migrationBuilder.DropTable(
                name: "SessionOccurrences",
                schema: "Academics");

            migrationBuilder.DropIndex(
                name: "IX_StudentPayments_PaymentCycleId_StudentId",
                schema: "Academics",
                table: "StudentPayments");

            migrationBuilder.DropColumn(
                name: "PaymentCycleId",
                schema: "Academics",
                table: "StudentPayments");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                schema: "Academics",
                table: "Sessions",
                newName: "IsRecurring");

            migrationBuilder.RenameColumn(
                name: "SessionsPerCycle",
                schema: "Academics",
                table: "Groups",
                newName: "MonthlySessionCount");

            migrationBuilder.RenameColumn(
                name: "OccurrenceId",
                schema: "Academics",
                table: "AttendanceRecords",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecords_OccurrenceId_StudentId",
                schema: "Academics",
                table: "AttendanceRecords",
                newName: "IX_AttendanceRecords_SessionId_StudentId");

            migrationBuilder.AddColumn<int>(
                name: "Month",
                schema: "Academics",
                table: "StudentPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                schema: "Academics",
                table: "StudentPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "QrToken",
                schema: "Academics",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SpecificDate",
                schema: "Academics",
                table: "Sessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "Academics",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StudentPayments_GroupId_StudentId_Month_Year",
                schema: "Academics",
                table: "StudentPayments",
                columns: new[] { "GroupId", "StudentId", "Month", "Year" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Sessions_SessionId",
                schema: "Academics",
                table: "AttendanceRecords",
                column: "SessionId",
                principalSchema: "Academics",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPayments_Groups_GroupId",
                schema: "Academics",
                table: "StudentPayments",
                column: "GroupId",
                principalSchema: "Academics",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
