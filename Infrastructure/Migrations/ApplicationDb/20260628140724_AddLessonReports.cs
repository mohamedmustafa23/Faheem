using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddLessonReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LessonReports",
                schema: "Academics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonTopic = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Homework = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonReports_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "Academics",
                        principalTable: "Groups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LessonReports_SessionOccurrences_OccurrenceId",
                        column: x => x.OccurrenceId,
                        principalSchema: "Academics",
                        principalTable: "SessionOccurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonReportEntries",
                schema: "Academics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Performance = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Participation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HomeworkResult = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonReportEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonReportEntries_LessonReports_LessonReportId",
                        column: x => x.LessonReportId,
                        principalSchema: "Academics",
                        principalTable: "LessonReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonReportEntries_LessonReportId_StudentId",
                schema: "Academics",
                table: "LessonReportEntries",
                columns: new[] { "LessonReportId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonReports_GroupId",
                schema: "Academics",
                table: "LessonReports",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonReports_OccurrenceId",
                schema: "Academics",
                table: "LessonReports",
                column: "OccurrenceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonReportEntries",
                schema: "Academics");

            migrationBuilder.DropTable(
                name: "LessonReports",
                schema: "Academics");
        }
    }
}
