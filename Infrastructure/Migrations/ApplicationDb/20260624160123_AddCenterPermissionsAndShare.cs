using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddCenterPermissionsAndShare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Permissions",
                schema: "Identity",
                table: "WorkspaceMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SharePercent",
                schema: "Identity",
                table: "WorkspaceMembers",
                type: "decimal(5,2)",
                nullable: true);

            // Existing workspace owners get full operational capability so they can act
            // immediately. Harmless for individual teachers (their toolkit already comes
            // from the Teacher role). 63 = CenterPermissions.All. Role is stored as text.
            migrationBuilder.Sql(
                "UPDATE [Identity].[WorkspaceMembers] SET [Permissions] = 63 WHERE [Role] = 'Owner';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Permissions",
                schema: "Identity",
                table: "WorkspaceMembers");

            migrationBuilder.DropColumn(
                name: "SharePercent",
                schema: "Identity",
                table: "WorkspaceMembers");
        }
    }
}
