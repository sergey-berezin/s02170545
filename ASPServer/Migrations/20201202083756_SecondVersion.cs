using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPServer.Migrations
{
    public partial class SecondVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Results_ResultData_ResultDataId",
                table: "Results");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResultData",
                table: "ResultData");

            migrationBuilder.RenameTable(
                name: "ResultData",
                newName: "ResultsData");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResultsData",
                table: "ResultsData",
                column: "ResultDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Results_ResultsData_ResultDataId",
                table: "Results",
                column: "ResultDataId",
                principalTable: "ResultsData",
                principalColumn: "ResultDataId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Results_ResultsData_ResultDataId",
                table: "Results");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResultsData",
                table: "ResultsData");

            migrationBuilder.RenameTable(
                name: "ResultsData",
                newName: "ResultData");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResultData",
                table: "ResultData",
                column: "ResultDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Results_ResultData_ResultDataId",
                table: "Results",
                column: "ResultDataId",
                principalTable: "ResultData",
                principalColumn: "ResultDataId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
