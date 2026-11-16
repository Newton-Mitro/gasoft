using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueConsolidationRunPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConsolidationRuns_TenantId_ConsolidationGroupId_PeriodEnd",
                table: "ConsolidationRuns");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationRuns_TenantId_ConsolidationGroupId_PeriodEnd",
                table: "ConsolidationRuns",
                columns: new[] { "TenantId", "ConsolidationGroupId", "PeriodEnd" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConsolidationRuns_TenantId_ConsolidationGroupId_PeriodEnd",
                table: "ConsolidationRuns");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationRuns_TenantId_ConsolidationGroupId_PeriodEnd",
                table: "ConsolidationRuns",
                columns: new[] { "TenantId", "ConsolidationGroupId", "PeriodEnd" });
        }
    }
}
