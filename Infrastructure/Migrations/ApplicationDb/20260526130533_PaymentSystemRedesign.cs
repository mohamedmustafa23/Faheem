using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class PaymentSystemRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentPayments",
                schema: "Academics");

            migrationBuilder.DropColumn(
                name: "CycleFee",
                schema: "Academics",
                table: "PaymentCycles");

            migrationBuilder.DropColumn(
                name: "IsStandalone",
                schema: "Academics",
                table: "PaymentCycles");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseFee",
                schema: "Academics",
                table: "PaymentCycles",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "StudentPaymentRecords",
                schema: "Academics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EnrolledAtSession = table.Column<int>(type: "int", nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentPaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentPaymentRecords_PaymentCycles_PaymentCycleId",
                        column: x => x.PaymentCycleId,
                        principalSchema: "Academics",
                        principalTable: "PaymentCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                schema: "Academics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPaymentRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_StudentPaymentRecords_StudentPaymentRecordId",
                        column: x => x.StudentPaymentRecordId,
                        principalSchema: "Academics",
                        principalTable: "StudentPaymentRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_StudentPaymentRecordId",
                schema: "Academics",
                table: "PaymentTransactions",
                column: "StudentPaymentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPaymentRecords_OccurrenceId_StudentId",
                schema: "Academics",
                table: "StudentPaymentRecords",
                columns: new[] { "OccurrenceId", "StudentId" },
                unique: true,
                filter: "[OccurrenceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPaymentRecords_PaymentCycleId_StudentId",
                schema: "Academics",
                table: "StudentPaymentRecords",
                columns: new[] { "PaymentCycleId", "StudentId" },
                unique: true,
                filter: "[PaymentCycleId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentTransactions",
                schema: "Academics");

            migrationBuilder.DropTable(
                name: "StudentPaymentRecords",
                schema: "Academics");

            migrationBuilder.DropColumn(
                name: "BaseFee",
                schema: "Academics",
                table: "PaymentCycles");

            migrationBuilder.AddColumn<decimal>(
                name: "CycleFee",
                schema: "Academics",
                table: "PaymentCycles",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsStandalone",
                schema: "Academics",
                table: "PaymentCycles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "StudentPayments",
                schema: "Academics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentPayments_PaymentCycles_PaymentCycleId",
                        column: x => x.PaymentCycleId,
                        principalSchema: "Academics",
                        principalTable: "PaymentCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentPayments_PaymentCycleId_StudentId",
                schema: "Academics",
                table: "StudentPayments",
                columns: new[] { "PaymentCycleId", "StudentId" },
                unique: true);
        }
    }
}
