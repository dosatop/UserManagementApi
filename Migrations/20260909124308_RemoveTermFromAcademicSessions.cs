using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTermFromAcademicSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Term",
                table: "AcademicSessions");

            migrationBuilder.CreateTable(
                name: "AcademicTerms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Term = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicTerms_AcademicSessions_AcademicSessionId",
                        column: x => x.AcademicSessionId,
                        principalTable: "AcademicSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicSessions_SchoolId_Session",
                table: "AcademicSessions",
                columns: new[] { "SchoolId", "Session" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicTerms_AcademicSessionId_IsCurrent",
                table: "AcademicTerms",
                columns: new[] { "AcademicSessionId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicTerms_AcademicSessionId_Term",
                table: "AcademicTerms",
                columns: new[] { "AcademicSessionId", "Term" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicTerms");

            migrationBuilder.DropIndex(
                name: "IX_AcademicSessions_SchoolId_Session",
                table: "AcademicSessions");

            migrationBuilder.AddColumn<string>(
                name: "Term",
                table: "AcademicSessions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
