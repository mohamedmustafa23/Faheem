using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class Add_ManualSession_PaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMode",
                schema: "Academics",
                table: "SessionOccurrences",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Free");

            migrationBuilder.AddColumn<decimal>(
                name: "SessionPrice",
                schema: "Academics",
                table: "SessionOccurrences",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CycleFee",
                schema: "Academics",
                table: "PaymentCycles",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraFee",
                schema: "Academics",
                table: "PaymentCycles",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsStandalone",
                schema: "Academics",
                table: "PaymentCycles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMode",
                schema: "Academics",
                table: "SessionOccurrences");

            migrationBuilder.DropColumn(
                name: "SessionPrice",
                schema: "Academics",
                table: "SessionOccurrences");

            migrationBuilder.DropColumn(
                name: "CycleFee",
                schema: "Academics",
                table: "PaymentCycles");

            migrationBuilder.DropColumn(
                name: "ExtraFee",
                schema: "Academics",
                table: "PaymentCycles");

            migrationBuilder.DropColumn(
                name: "IsStandalone",
                schema: "Academics",
                table: "PaymentCycles");
        }
    }
}
