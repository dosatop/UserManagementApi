using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTeacherSubjectAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TeacherSubjects",
                table: "TeacherSubjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeacherClasses",
                table: "TeacherClasses");

            migrationBuilder.DropIndex(
                name: "IX_TeacherClasses_ClassId",
                table: "TeacherClasses");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClassId",
                table: "TeacherSubjects",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "TeacherSubjects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "TeacherClasses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeacherSubjects",
                table: "TeacherSubjects",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeacherClasses",
                table: "TeacherClasses",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_TeacherId_SubjectId_ClassId",
                table: "TeacherSubjects",
                columns: new[] { "TeacherId", "SubjectId", "ClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherClasses_ClassId",
                table: "TeacherClasses",
                column: "ClassId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherClasses_TeacherId",
                table: "TeacherClasses",
                column: "TeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TeacherSubjects",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjects_TeacherId_SubjectId_ClassId",
                table: "TeacherSubjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeacherClasses",
                table: "TeacherClasses");

            migrationBuilder.DropIndex(
                name: "IX_TeacherClasses_ClassId",
                table: "TeacherClasses");

            migrationBuilder.DropIndex(
                name: "IX_TeacherClasses_TeacherId",
                table: "TeacherClasses");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "TeacherClasses");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClassId",
                table: "TeacherSubjects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeacherSubjects",
                table: "TeacherSubjects",
                columns: new[] { "TeacherId", "SubjectId", "ClassId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeacherClasses",
                table: "TeacherClasses",
                columns: new[] { "TeacherId", "ClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherClasses_ClassId",
                table: "TeacherClasses",
                column: "ClassId");
        }
    }
}
