using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddWorkspaceModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                schema: "Academics",
                table: "Groups",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkspaceMembers",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceMembers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OwnerUserId",
                schema: "Academics",
                table: "Groups",
                column: "OwnerUserId",
                filter: "[OwnerUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMembers_TenantId",
                schema: "Identity",
                table: "WorkspaceMembers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMembers_UserId_TenantId",
                schema: "Identity",
                table: "WorkspaceMembers",
                columns: new[] { "UserId", "TenantId" },
                unique: true);

            // ── Data backfill ────────────────────────────────────────────────────
            // Membership used to be expressed as a single "tenant" user-claim. Promote
            // every existing claim into a WorkspaceMember row: a user who holds the
            // Assistant role becomes an Assistant member, everyone else (teachers, the
            // root admin) becomes the Owner of that workspace. Idempotent — re-running
            // skips rows that already exist.
            migrationBuilder.Sql(@"
                INSERT INTO [Identity].[WorkspaceMembers] ([Id], [UserId], [TenantId], [Role], [Status], [CreatedAt])
                SELECT NEWID(), uc.[UserId], uc.[ClaimValue],
                       CASE WHEN EXISTS (
                           SELECT 1 FROM [Identity].[UserRoles] ur
                           INNER JOIN [Identity].[Roles] r ON r.[Id] = ur.[RoleId]
                           WHERE ur.[UserId] = uc.[UserId] AND r.[NormalizedName] = N'ASSISTANT'
                       ) THEN N'Assistant' ELSE N'Owner' END,
                       N'Active', SYSUTCDATETIME()
                FROM [Identity].[UserClaims] uc
                WHERE uc.[ClaimType] = N'tenant'
                  AND NOT EXISTS (
                      SELECT 1 FROM [Identity].[WorkspaceMembers] wm
                      WHERE wm.[UserId] = uc.[UserId] AND wm.[TenantId] = uc.[ClaimValue]
                  );");

            // Stamp each existing group with its owning teacher (the workspace Owner).
            // For today's single-teacher tenants this is unambiguous.
            migrationBuilder.Sql(@"
                UPDATE g
                SET g.[OwnerUserId] = wm.[UserId]
                FROM [Academics].[Groups] g
                INNER JOIN [Identity].[WorkspaceMembers] wm
                    ON wm.[TenantId] = g.[TenantId] AND wm.[Role] = N'Owner'
                WHERE g.[OwnerUserId] IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkspaceMembers",
                schema: "Identity");

            migrationBuilder.DropIndex(
                name: "IX_Groups_OwnerUserId",
                schema: "Academics",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "Academics",
                table: "Groups");
        }
    }
}
