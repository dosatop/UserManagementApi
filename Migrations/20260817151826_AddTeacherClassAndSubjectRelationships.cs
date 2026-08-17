using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherClassAndSubjectRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Subjects_SubjectId1",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Teachers_TeacherId1",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Subjects_SubjectId1",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Teachers_TeacherId1",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_Schools_SchoolId",
                table: "Teachers");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_Subjects_SubjectId",
                table: "TeacherSubjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeacherSubjects",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_Teachers_SchoolId_EmployeeNumber",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_SubjectId1",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_TeacherId1",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_SubjectId1",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_TeacherId1",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "SubjectId1",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "TeacherId1",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "SubjectId1",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "TeacherId1",
                table: "Assignments");

            migrationBuilder.AddColumn<Guid>(
                name: "ClassId",
                table: "TeacherSubjects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeacherSubjects",
                table: "TeacherSubjects",
                columns: new[] { "TeacherId", "SubjectId", "ClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_ClassId",
                table: "TeacherSubjects",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_SchoolId",
                table: "Teachers",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_Schools_SchoolId",
                table: "Teachers",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_Classes_ClassId",
                table: "TeacherSubjects",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_Subjects_SubjectId",
                table: "TeacherSubjects",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_Schools_SchoolId",
                table: "Teachers");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_Classes_ClassId",
                table: "TeacherSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_Subjects_SubjectId",
                table: "TeacherSubjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeacherSubjects",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjects_ClassId",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_Teachers_SchoolId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "TeacherSubjects");

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectId1",
                table: "AttendanceRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeacherId1",
                table: "AttendanceRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectId1",
                table: "Assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeacherId1",
                table: "Assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeacherSubjects",
                table: "TeacherSubjects",
                columns: new[] { "TeacherId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_SchoolId_EmployeeNumber",
                table: "Teachers",
                columns: new[] { "SchoolId", "EmployeeNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_SubjectId1",
                table: "AttendanceRecords",
                column: "SubjectId1");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_TeacherId1",
                table: "AttendanceRecords",
                column: "TeacherId1");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_SubjectId1",
                table: "Assignments",
                column: "SubjectId1");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TeacherId1",
                table: "Assignments",
                column: "TeacherId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Subjects_SubjectId1",
                table: "Assignments",
                column: "SubjectId1",
                principalTable: "Subjects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Teachers_TeacherId1",
                table: "Assignments",
                column: "TeacherId1",
                principalTable: "Teachers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Subjects_SubjectId1",
                table: "AttendanceRecords",
                column: "SubjectId1",
                principalTable: "Subjects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Teachers_TeacherId1",
                table: "AttendanceRecords",
                column: "TeacherId1",
                principalTable: "Teachers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_Schools_SchoolId",
                table: "Teachers",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_Subjects_SubjectId",
                table: "TeacherSubjects",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
