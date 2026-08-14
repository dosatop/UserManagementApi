using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentPortalAttendanceAndAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignment_Classes_ClassId",
                table: "Assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignment_Schools_SchoolId",
                table: "Assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignment_Subjects_SubjectId",
                table: "Assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignment_Teachers_TeacherId",
                table: "Assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentSubmission_Assignment_AssignmentId",
                table: "AssignmentSubmission");

            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentSubmission_StudentProfiles_StudentId",
                table: "AssignmentSubmission");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecord_Classes_ClassId",
                table: "AttendanceRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecord_Schools_SchoolId",
                table: "AttendanceRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecord_StudentProfiles_StudentId",
                table: "AttendanceRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecord_Subjects_SubjectId",
                table: "AttendanceRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecord_Teachers_TeacherId",
                table: "AttendanceRecord");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AttendanceRecord",
                table: "AttendanceRecord");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AssignmentSubmission",
                table: "AssignmentSubmission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Assignment",
                table: "Assignment");

            migrationBuilder.RenameTable(
                name: "AttendanceRecord",
                newName: "AttendanceRecords");

            migrationBuilder.RenameTable(
                name: "AssignmentSubmission",
                newName: "AssignmentSubmissions");

            migrationBuilder.RenameTable(
                name: "Assignment",
                newName: "Assignments");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecord_TeacherId",
                table: "AttendanceRecords",
                newName: "IX_AttendanceRecords_TeacherId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecord_SubjectId",
                table: "AttendanceRecords",
                newName: "IX_AttendanceRecords_SubjectId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecord_StudentId",
                table: "AttendanceRecords",
                newName: "IX_AttendanceRecords_StudentId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecord_SchoolId",
                table: "AttendanceRecords",
                newName: "IX_AttendanceRecords_SchoolId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecord_ClassId",
                table: "AttendanceRecords",
                newName: "IX_AttendanceRecords_ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_AssignmentSubmission_StudentId",
                table: "AssignmentSubmissions",
                newName: "IX_AssignmentSubmissions_StudentId");

            migrationBuilder.RenameIndex(
                name: "IX_AssignmentSubmission_AssignmentId",
                table: "AssignmentSubmissions",
                newName: "IX_AssignmentSubmissions_AssignmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Assignment_TeacherId",
                table: "Assignments",
                newName: "IX_Assignments_TeacherId");

            migrationBuilder.RenameIndex(
                name: "IX_Assignment_SubjectId",
                table: "Assignments",
                newName: "IX_Assignments_SubjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Assignment_SchoolId",
                table: "Assignments",
                newName: "IX_Assignments_SchoolId");

            migrationBuilder.RenameIndex(
                name: "IX_Assignment_ClassId",
                table: "Assignments",
                newName: "IX_Assignments_ClassId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AttendanceRecords",
                table: "AttendanceRecords",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssignmentSubmissions",
                table: "AssignmentSubmissions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Assignments",
                table: "Assignments",
                column: "Id");

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
                name: "FK_AssignmentSubmissions_Assignments_AssignmentId",
                table: "AssignmentSubmissions",
                column: "AssignmentId",
                principalTable: "Assignments",
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
                name: "FK_AttendanceRecords_StudentProfiles_StudentId",
                table: "AttendanceRecords",
                column: "StudentId",
                principalTable: "StudentProfiles",
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
                name: "FK_Assignments_Teachers_TeacherId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentSubmissions_Assignments_AssignmentId",
                table: "AssignmentSubmissions");

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
                name: "FK_AttendanceRecords_StudentProfiles_StudentId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Subjects_SubjectId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Teachers_TeacherId",
                table: "AttendanceRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AttendanceRecords",
                table: "AttendanceRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AssignmentSubmissions",
                table: "AssignmentSubmissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Assignments",
                table: "Assignments");

            migrationBuilder.RenameTable(
                name: "AttendanceRecords",
                newName: "AttendanceRecord");

            migrationBuilder.RenameTable(
                name: "AssignmentSubmissions",
                newName: "AssignmentSubmission");

            migrationBuilder.RenameTable(
                name: "Assignments",
                newName: "Assignment");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecords_TeacherId",
                table: "AttendanceRecord",
                newName: "IX_AttendanceRecord_TeacherId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecords_SubjectId",
                table: "AttendanceRecord",
                newName: "IX_AttendanceRecord_SubjectId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecords_StudentId",
                table: "AttendanceRecord",
                newName: "IX_AttendanceRecord_StudentId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecords_SchoolId",
                table: "AttendanceRecord",
                newName: "IX_AttendanceRecord_SchoolId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecords_ClassId",
                table: "AttendanceRecord",
                newName: "IX_AttendanceRecord_ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_AssignmentSubmissions_StudentId",
                table: "AssignmentSubmission",
                newName: "IX_AssignmentSubmission_StudentId");

            migrationBuilder.RenameIndex(
                name: "IX_AssignmentSubmissions_AssignmentId",
                table: "AssignmentSubmission",
                newName: "IX_AssignmentSubmission_AssignmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Assignments_TeacherId",
                table: "Assignment",
                newName: "IX_Assignment_TeacherId");

            migrationBuilder.RenameIndex(
                name: "IX_Assignments_SubjectId",
                table: "Assignment",
                newName: "IX_Assignment_SubjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Assignments_SchoolId",
                table: "Assignment",
                newName: "IX_Assignment_SchoolId");

            migrationBuilder.RenameIndex(
                name: "IX_Assignments_ClassId",
                table: "Assignment",
                newName: "IX_Assignment_ClassId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AttendanceRecord",
                table: "AttendanceRecord",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssignmentSubmission",
                table: "AssignmentSubmission",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Assignment",
                table: "Assignment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignment_Classes_ClassId",
                table: "Assignment",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignment_Schools_SchoolId",
                table: "Assignment",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignment_Subjects_SubjectId",
                table: "Assignment",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignment_Teachers_TeacherId",
                table: "Assignment",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentSubmission_Assignment_AssignmentId",
                table: "AssignmentSubmission",
                column: "AssignmentId",
                principalTable: "Assignment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentSubmission_StudentProfiles_StudentId",
                table: "AssignmentSubmission",
                column: "StudentId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecord_Classes_ClassId",
                table: "AttendanceRecord",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecord_Schools_SchoolId",
                table: "AttendanceRecord",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecord_StudentProfiles_StudentId",
                table: "AttendanceRecord",
                column: "StudentId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecord_Subjects_SubjectId",
                table: "AttendanceRecord",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecord_Teachers_TeacherId",
                table: "AttendanceRecord",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
