using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentsTradesAndSubjectTypeDBContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Department_Schools_SchoolId",
                table: "Department");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Department_DepartmentId",
                table: "Subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Trade_TradeId",
                table: "Subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_Trade_Schools_SchoolId",
                table: "Trade");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Trade",
                table: "Trade");

            migrationBuilder.DropIndex(
                name: "IX_Trade_SchoolId",
                table: "Trade");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Department",
                table: "Department");

            migrationBuilder.DropIndex(
                name: "IX_Department_SchoolId",
                table: "Department");

            migrationBuilder.RenameTable(
                name: "Trade",
                newName: "Trades");

            migrationBuilder.RenameTable(
                name: "Department",
                newName: "Departments");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trades",
                table: "Trades",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Departments",
                table: "Departments",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_SchoolId_Name",
                table: "Trades",
                columns: new[] { "SchoolId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_SchoolId_Name",
                table: "Departments",
                columns: new[] { "SchoolId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Schools_SchoolId",
                table: "Departments",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Departments_DepartmentId",
                table: "Subjects",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Trades_TradeId",
                table: "Subjects",
                column: "TradeId",
                principalTable: "Trades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Schools_SchoolId",
                table: "Trades",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Schools_SchoolId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Departments_DepartmentId",
                table: "Subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Trades_TradeId",
                table: "Subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_Trades_Schools_SchoolId",
                table: "Trades");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Trades",
                table: "Trades");

            migrationBuilder.DropIndex(
                name: "IX_Trades_SchoolId_Name",
                table: "Trades");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Departments",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_SchoolId_Name",
                table: "Departments");

            migrationBuilder.RenameTable(
                name: "Trades",
                newName: "Trade");

            migrationBuilder.RenameTable(
                name: "Departments",
                newName: "Department");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trade",
                table: "Trade",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Department",
                table: "Department",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Trade_SchoolId",
                table: "Trade",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_SchoolId",
                table: "Department",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Department_Schools_SchoolId",
                table: "Department",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Trade_Schools_SchoolId",
                table: "Trade",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
