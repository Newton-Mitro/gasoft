using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SalesQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesQuotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    SalesInvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    QuoteNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    QuoteDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    NetTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotes", x => x.Id);
                    table.UniqueConstraint("AK_SalesQuotes_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_SalesQuotes_Totals", "\"NetTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"GrandTotal\" = \"NetTotal\" + \"TaxTotal\"");
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Parties_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalTable: "Parties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_SalesInvoices_TenantId_CompanyId_SalesInvoiceId",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesInvoiceId },
                        principalTable: "SalesInvoices",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuoteLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SalesQuoteId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    TaxCodeId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TaxRatePercent = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuoteLines", x => x.Id);
                    table.CheckConstraint("CK_SalesQuoteLines_Amounts", "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"NetAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"GrossAmount\" = \"NetAmount\" + \"TaxAmount\"");
                    table.ForeignKey(
                        name: "FK_SalesQuoteLines_ChartOfAccounts_TenantId_CompanyId_AccountId",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuoteLines_SalesQuotes_TenantId_CompanyId_SalesQuoteId",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesQuoteId },
                        principalTable: "SalesQuotes",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesQuoteLines_TaxCodes_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuoteLines_TaxCodeId",
                table: "SalesQuoteLines",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuoteLines_TenantId_CompanyId_AccountId",
                table: "SalesQuoteLines",
                columns: new[] { "TenantId", "CompanyId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuoteLines_TenantId_CompanyId_SalesQuoteId",
                table: "SalesQuoteLines",
                columns: new[] { "TenantId", "CompanyId", "SalesQuoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_CurrencyCode",
                table: "SalesQuotes",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_TenantId_CompanyId_QuoteNumber",
                table: "SalesQuotes",
                columns: new[] { "TenantId", "CompanyId", "QuoteNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_TenantId_CompanyId_SalesInvoiceId",
                table: "SalesQuotes",
                columns: new[] { "TenantId", "CompanyId", "SalesInvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_TenantId_CustomerId",
                table: "SalesQuotes",
                columns: new[] { "TenantId", "CustomerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesQuoteLines");

            migrationBuilder.DropTable(
                name: "SalesQuotes");
        }
    }
}
