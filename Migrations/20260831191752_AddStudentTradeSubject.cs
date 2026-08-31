using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentTradeSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TradeSubjectId",
                table: "StudentProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentProfiles_TradeSubjectId",
                table: "StudentProfiles",
                column: "TradeSubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentProfiles_Subjects_TradeSubjectId",
                table: "StudentProfiles",
                column: "TradeSubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentProfiles_Subjects_TradeSubjectId",
                table: "StudentProfiles");

            migrationBuilder.DropIndex(
                name: "IX_StudentProfiles_TradeSubjectId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "TradeSubjectId",
                table: "StudentProfiles");
        }
    }
}
