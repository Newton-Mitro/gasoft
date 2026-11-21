using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixExpenseReportLineRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseReportLines_ExpenseReports_ExpenseReportId",
                table: "ExpenseReportLines");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseReportLines_ExpenseReportId",
                table: "ExpenseReportLines");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReportLines_ExpenseReportId",
                table: "ExpenseReportLines",
                column: "ExpenseReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseReportLines_ExpenseReports_ExpenseReportId",
                table: "ExpenseReportLines",
                column: "ExpenseReportId",
                principalTable: "ExpenseReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
