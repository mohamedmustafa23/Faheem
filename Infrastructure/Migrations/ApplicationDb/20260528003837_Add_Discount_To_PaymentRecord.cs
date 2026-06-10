using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class Add_Discount_To_PaymentRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                schema: "Academics",
                table: "StudentPaymentRecords",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountReason",
                schema: "Academics",
                table: "StudentPaymentRecords",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_StudentPaymentRecord_DiscountValid",
                schema: "Academics",
                table: "StudentPaymentRecords",
                sql: "[DiscountAmount] >= 0 AND [DiscountAmount] <= [ExpectedAmount]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StudentPaymentRecord_DiscountValid",
                schema: "Academics",
                table: "StudentPaymentRecords");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                schema: "Academics",
                table: "StudentPaymentRecords");

            migrationBuilder.DropColumn(
                name: "DiscountReason",
                schema: "Academics",
                table: "StudentPaymentRecords");
        }
    }
}
