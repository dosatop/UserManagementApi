using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class FixAssignmentRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Classes_ClassId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Schools_SchoolId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Subjects_SubjectId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Teachers_TeacherId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentSubmissions_StudentProfiles_StudentId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Classes_ClassId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Schools_SchoolId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Subjects_SubjectId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Teachers_TeacherId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_StudentId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentSubmissions_AssignmentId",
                table: "AssignmentSubmissions");

            migrationBuilder.AddColumn<Guid>(
                name: "StudentProfileId",
                table: "AttendanceRecords",
                type: "uuid",
                nullable: true);

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
                name: "StudentProfileId",
                table: "AssignmentSubmissions",
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

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId_ClassId_SubjectId_AttendanceDat~",
                table: "AttendanceRecords",
                columns: new[] { "StudentId", "ClassId", "SubjectId", "AttendanceDate", "Session", "Term" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentProfileId",
                table: "AttendanceRecords",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_SubjectId1",
                table: "AttendanceRecords",
                column: "SubjectId1");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_TeacherId1",
                table: "AttendanceRecords",
                column: "TeacherId1");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_AssignmentId_StudentId",
                table: "AssignmentSubmissions",
                columns: new[] { "AssignmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_StudentProfileId",
                table: "AssignmentSubmissions",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_SubjectId1",
                table: "Assignments",
                column: "SubjectId1");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TeacherId1",
                table: "Assignments",
                column: "TeacherId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Classes_ClassId",
                table: "Assignments",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Schools_SchoolId",
                table: "Assignments",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Subjects_SubjectId",
                table: "Assignments",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Subjects_SubjectId1",
                table: "Assignments",
                column: "SubjectId1",
                principalTable: "Subjects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Teachers_TeacherId",
                table: "Assignments",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Teachers_TeacherId1",
                table: "Assignments",
                column: "TeacherId1",
                principalTable: "Teachers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentSubmissions_StudentProfiles_StudentId",
                table: "AssignmentSubmissions",
                column: "StudentId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentSubmissions_StudentProfiles_StudentProfileId",
                table: "AssignmentSubmissions",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Classes_ClassId",
                table: "AttendanceRecords",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Schools_SchoolId",
                table: "AttendanceRecords",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_StudentProfiles_StudentProfileId",
                table: "AttendanceRecords",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Subjects_SubjectId",
                table: "AttendanceRecords",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Subjects_SubjectId1",
                table: "AttendanceRecords",
                column: "SubjectId1",
                principalTable: "Subjects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Teachers_TeacherId",
                table: "AttendanceRecords",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Teachers_TeacherId1",
                table: "AttendanceRecords",
                column: "TeacherId1",
                principalTable: "Teachers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Classes_ClassId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Schools_SchoolId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Subjects_SubjectId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Subjects_SubjectId1",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Teachers_TeacherId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Teachers_TeacherId1",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentSubmissions_StudentProfiles_StudentId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentSubmissions_StudentProfiles_StudentProfileId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Classes_ClassId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Schools_SchoolId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_StudentProfiles_StudentProfileId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Subjects_SubjectId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Subjects_SubjectId1",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Teachers_TeacherId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Teachers_TeacherId1",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_StudentId_ClassId_SubjectId_AttendanceDat~",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_StudentProfileId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_SubjectId1",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_TeacherId1",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentSubmissions_AssignmentId_StudentId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentSubmissions_StudentProfileId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_SubjectId1",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_TeacherId1",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "StudentProfileId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "SubjectId1",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "TeacherId1",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "StudentProfileId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "SubjectId1",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "TeacherId1",
                table: "Assignments");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId",
                table: "AttendanceRecords",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_AssignmentId",
                table: "AssignmentSubmissions",
                column: "AssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Classes_ClassId",
                table: "Assignments",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Schools_SchoolId",
                table: "Assignments",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Subjects_SubjectId",
                table: "Assignments",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Teachers_TeacherId",
                table: "Assignments",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentSubmissions_StudentProfiles_StudentId",
                table: "AssignmentSubmissions",
                column: "StudentId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Classes_ClassId",
                table: "AttendanceRecords",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Schools_SchoolId",
                table: "AttendanceRecords",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Subjects_SubjectId",
                table: "AttendanceRecords",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Teachers_TeacherId",
                table: "AttendanceRecords",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
