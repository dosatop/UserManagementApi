using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentDepartmentTradeAndSchoolLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "StudentProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TradeId",
                table: "StudentProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "Classes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ClassSubjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassSubjects_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassSubjects_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassSubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentProfiles_DepartmentId",
                table: "StudentProfiles",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentProfiles_TradeId",
                table: "StudentProfiles",
                column: "TradeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSubjects_ClassId_SubjectId",
                table: "ClassSubjects",
                columns: new[] { "ClassId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSubjects_SchoolId",
                table: "ClassSubjects",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSubjects_SubjectId",
                table: "ClassSubjects",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentProfiles_Departments_DepartmentId",
                table: "StudentProfiles",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentProfiles_Trades_TradeId",
                table: "StudentProfiles",
                column: "TradeId",
                principalTable: "Trades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentProfiles_Departments_DepartmentId",
                table: "StudentProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentProfiles_Trades_TradeId",
                table: "StudentProfiles");

            migrationBuilder.DropTable(
                name: "ClassSubjects");

            migrationBuilder.DropIndex(
                name: "IX_StudentProfiles_DepartmentId",
                table: "StudentProfiles");

            migrationBuilder.DropIndex(
                name: "IX_StudentProfiles_TradeId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "TradeId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Classes");
        }
    }
}
