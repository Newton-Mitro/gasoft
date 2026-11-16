using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BankReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_JournalLines_TenantId_CompanyId_Id",
                table: "JournalLines",
                columns: new[] { "TenantId", "CompanyId", "Id" });

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    ChartOfAccountId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BankName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AccountNumberLastFour = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                    table.UniqueConstraint("AK_BankAccounts_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_BankAccounts_ChartOfAccounts_TenantId_CompanyId_ChartOfAcco~",
                        columns: x => new { x.TenantId, x.CompanyId, x.ChartOfAccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankAccounts_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankAccounts_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankStatementTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    BankAccountId = table.Column<long>(type: "bigint", nullable: false),
                    JournalLineId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ImportKey = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankStatementTransactions", x => x.Id);
                    table.UniqueConstraint("AK_BankStatementTransactions_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_BankStatementTransactions_Amount", "\"Amount\" <> 0");
                    table.ForeignKey(
                        name: "FK_BankStatementTransactions_BankAccounts_TenantId_CompanyId_B~",
                        columns: x => new { x.TenantId, x.CompanyId, x.BankAccountId },
                        principalTable: "BankAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BankStatementTransactions_JournalLines_TenantId_CompanyId_J~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalLineId },
                        principalTable: "JournalLines",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_CurrencyCode",
                table: "BankAccounts",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_TenantId_CompanyId_ChartOfAccountId",
                table: "BankAccounts",
                columns: new[] { "TenantId", "CompanyId", "ChartOfAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementTransactions_TenantId_CompanyId_BankAccountId_~",
                table: "BankStatementTransactions",
                columns: new[] { "TenantId", "CompanyId", "BankAccountId", "ImportKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementTransactions_TenantId_CompanyId_JournalLineId",
                table: "BankStatementTransactions",
                columns: new[] { "TenantId", "CompanyId", "JournalLineId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankStatementTransactions");

            migrationBuilder.DropTable(
                name: "BankAccounts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_JournalLines_TenantId_CompanyId_Id",
                table: "JournalLines");
        }
    }
}
