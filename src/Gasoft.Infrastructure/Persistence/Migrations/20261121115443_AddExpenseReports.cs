using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseReports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    EmployeeUserId = table.Column<long>(type: "bigint", nullable: false),
                    ReviewerUserId = table.Column<long>(type: "bigint", nullable: true),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    ReimbursementJournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    EmployeePayableAccountId = table.Column<long>(type: "bigint", nullable: true),
                    ReimbursementCashAccountId = table.Column<long>(type: "bigint", nullable: true),
                    ReportNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReimbursedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseReports", x => x.Id);
                    table.UniqueConstraint("AK_ExpenseReports_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_ExpenseReports_TotalAmount", "\"TotalAmount\" > 0");
                    table.ForeignKey(
                        name: "FK_ExpenseReports_ChartOfAccounts_TenantId_CompanyId_EmployeeP~",
                        columns: x => new { x.TenantId, x.CompanyId, x.EmployeePayableAccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseReports_ChartOfAccounts_TenantId_CompanyId_Reimburse~",
                        columns: x => new { x.TenantId, x.CompanyId, x.ReimbursementCashAccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseReports_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseReports_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseReports_JournalEntries_TenantId_CompanyId_JournalEnt~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseReports_JournalEntries_TenantId_CompanyId_Reimbursem~",
                        columns: x => new { x.TenantId, x.CompanyId, x.ReimbursementJournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseReportLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    ExpenseReportId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    ExpenseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Merchant = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ReceiptFileName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseReportLines", x => x.Id);
                    table.CheckConstraint("CK_ExpenseReportLines_Amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_ExpenseReportLines_ChartOfAccounts_TenantId_CompanyId_Accou~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseReportLines_ExpenseReports_ExpenseReportId",
                        column: x => x.ExpenseReportId,
                        principalTable: "ExpenseReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpenseReportLines_ExpenseReports_TenantId_CompanyId_Expens~",
                        columns: x => new { x.TenantId, x.CompanyId, x.ExpenseReportId },
                        principalTable: "ExpenseReports",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReportLines_ExpenseReportId",
                table: "ExpenseReportLines",
                column: "ExpenseReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReportLines_TenantId_CompanyId_AccountId",
                table: "ExpenseReportLines",
                columns: new[] { "TenantId", "CompanyId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReportLines_TenantId_CompanyId_ExpenseReportId",
                table: "ExpenseReportLines",
                columns: new[] { "TenantId", "CompanyId", "ExpenseReportId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_CurrencyCode",
                table: "ExpenseReports",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_TenantId_CompanyId_EmployeePayableAccountId",
                table: "ExpenseReports",
                columns: new[] { "TenantId", "CompanyId", "EmployeePayableAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_TenantId_CompanyId_JournalEntryId",
                table: "ExpenseReports",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_TenantId_CompanyId_ReimbursementCashAccountId",
                table: "ExpenseReports",
                columns: new[] { "TenantId", "CompanyId", "ReimbursementCashAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_TenantId_CompanyId_ReimbursementJournalEntry~",
                table: "ExpenseReports",
                columns: new[] { "TenantId", "CompanyId", "ReimbursementJournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_TenantId_CompanyId_ReportNumber",
                table: "ExpenseReports",
                columns: new[] { "TenantId", "CompanyId", "ReportNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseReportLines");

            migrationBuilder.DropTable(
                name: "ExpenseReports");
        }
    }
}
