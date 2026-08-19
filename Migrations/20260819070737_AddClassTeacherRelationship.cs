using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class AddClassTeacherRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeacherClasses_ClassId",
                table: "TeacherClasses");

            migrationBuilder.DropIndex(
                name: "IX_TeacherClasses_TeacherId",
                table: "TeacherClasses");

            migrationBuilder.DropIndex(
                name: "IX_ClassTeachers_ClassId",
                table: "ClassTeachers");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherClasses_ClassId",
                table: "TeacherClasses",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherClasses_TeacherId_ClassId",
                table: "TeacherClasses",
                columns: new[] { "TeacherId", "ClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassTeachers_ClassId",
                table: "ClassTeachers",
                column: "ClassId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassTeachers_SchoolId",
                table: "ClassTeachers",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassTeachers_Schools_SchoolId",
                table: "ClassTeachers",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassTeachers_Schools_SchoolId",
                table: "ClassTeachers");

            migrationBuilder.DropIndex(
                name: "IX_TeacherClasses_ClassId",
                table: "TeacherClasses");

            migrationBuilder.DropIndex(
                name: "IX_TeacherClasses_TeacherId_ClassId",
                table: "TeacherClasses");

            migrationBuilder.DropIndex(
                name: "IX_ClassTeachers_ClassId",
                table: "ClassTeachers");

            migrationBuilder.DropIndex(
                name: "IX_ClassTeachers_SchoolId",
                table: "ClassTeachers");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherClasses_ClassId",
                table: "TeacherClasses",
                column: "ClassId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherClasses_TeacherId",
                table: "TeacherClasses",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassTeachers_ClassId",
                table: "ClassTeachers",
                column: "ClassId");
        }
    }
}
