using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class MakeStudentClassRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentProfiles_Classes_ClassId",
                table: "StudentProfiles");

            migrationBuilder.CreateTable(
                name: "AcademicSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Session = table.Column<string>(type: "text", nullable: false),
                    Term = table.Column<string>(type: "text", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicSessions_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicSessions_SchoolId_IsCurrent",
                table: "AcademicSessions",
                columns: new[] { "SchoolId", "IsCurrent" });

            migrationBuilder.AddForeignKey(
                name: "FK_StudentProfiles_Classes_ClassId",
                table: "StudentProfiles",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentProfiles_Classes_ClassId",
                table: "StudentProfiles");

            migrationBuilder.DropTable(
                name: "AcademicSessions");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentProfiles_Classes_ClassId",
                table: "StudentProfiles",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
