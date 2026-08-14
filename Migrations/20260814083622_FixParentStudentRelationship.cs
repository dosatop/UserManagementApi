using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class FixParentStudentRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParentStudents_ParentProfile_ParentId",
                table: "ParentStudents");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentStudents_Parents_ParentId1",
                table: "ParentStudents");

            migrationBuilder.DropTable(
                name: "ParentProfile");

            migrationBuilder.DropIndex(
                name: "IX_ParentStudents_ParentId1",
                table: "ParentStudents");

            migrationBuilder.DropColumn(
                name: "ParentId1",
                table: "ParentStudents");

            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "StudentResults",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_ParentStudents_Parents_ParentId",
                table: "ParentStudents",
                column: "ParentId",
                principalTable: "Parents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParentStudents_Parents_ParentId",
                table: "ParentStudents");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "StudentResults");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId1",
                table: "ParentStudents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ParentProfile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentProfile_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParentProfile_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParentStudents_ParentId1",
                table: "ParentStudents",
                column: "ParentId1");

            migrationBuilder.CreateIndex(
                name: "IX_ParentProfile_SchoolId",
                table: "ParentProfile",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentProfile_UserId",
                table: "ParentProfile",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParentStudents_ParentProfile_ParentId",
                table: "ParentStudents",
                column: "ParentId",
                principalTable: "ParentProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentStudents_Parents_ParentId1",
                table: "ParentStudents",
                column: "ParentId1",
                principalTable: "Parents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
