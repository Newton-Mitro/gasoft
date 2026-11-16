using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Budgeting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Budgets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    FiscalYearId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.Id);
                    table.UniqueConstraint("AK_Budgets_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_Budgets_FiscalYears_TenantId_CompanyId_FiscalYearId",
                        columns: x => new { x.TenantId, x.CompanyId, x.FiscalYearId },
                        principalTable: "FiscalYears",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    BudgetId = table.Column<long>(type: "bigint", nullable: false),
                    AccountingPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetLines", x => x.Id);
                    table.CheckConstraint("CK_BudgetLines_Amount", "\"Amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_BudgetLines_AccountingPeriods_TenantId_CompanyId_Accounting~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetLines_Budgets_TenantId_CompanyId_BudgetId",
                        columns: x => new { x.TenantId, x.CompanyId, x.BudgetId },
                        principalTable: "Budgets",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetLines_ChartOfAccounts_TenantId_CompanyId_AccountId",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_TenantId_CompanyId_AccountId",
                table: "BudgetLines",
                columns: new[] { "TenantId", "CompanyId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_TenantId_CompanyId_AccountingPeriodId",
                table: "BudgetLines",
                columns: new[] { "TenantId", "CompanyId", "AccountingPeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_TenantId_CompanyId_BudgetId_AccountingPeriodId_~",
                table: "BudgetLines",
                columns: new[] { "TenantId", "CompanyId", "BudgetId", "AccountingPeriodId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_CompanyId_FiscalYearId_Name",
                table: "Budgets",
                columns: new[] { "TenantId", "CompanyId", "FiscalYearId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetLines");

            migrationBuilder.DropTable(
                name: "Budgets");
        }
    }
}
