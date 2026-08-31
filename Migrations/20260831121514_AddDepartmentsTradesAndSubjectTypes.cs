using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentsTradesAndSubjectTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Subjects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TradeId",
                table: "Subjects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Subjects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Department",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Department_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trade_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_DepartmentId",
                table: "Subjects",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_TradeId",
                table: "Subjects",
                column: "TradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_SchoolId",
                table: "Department",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Trade_SchoolId",
                table: "Trade",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Department_DepartmentId",
                table: "Subjects",
                column: "DepartmentId",
                principalTable: "Department",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Trade_TradeId",
                table: "Subjects",
                column: "TradeId",
                principalTable: "Trade",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Department_DepartmentId",
                table: "Subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Trade_TradeId",
                table: "Subjects");

            migrationBuilder.DropTable(
                name: "Department");

            migrationBuilder.DropTable(
                name: "Trade");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_DepartmentId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_TradeId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "TradeId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Subjects");
        }
    }
}
