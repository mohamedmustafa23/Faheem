using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxTeachers",
                schema: "Multitenant",
                table: "Tenants",
                type: "int",
                nullable: true);

            // All existing tenants are single-teacher workspaces → backfill as Individual.
            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "Multitenant",
                table: "Tenants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Individual");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxTeachers",
                schema: "Multitenant",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "Multitenant",
                table: "Tenants");
        }
    }
}
