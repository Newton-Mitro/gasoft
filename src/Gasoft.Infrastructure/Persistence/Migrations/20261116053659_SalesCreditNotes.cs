using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SalesCreditNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_SalesInvoiceLines_TenantId_CompanyId_Id",
                table: "SalesInvoiceLines",
                columns: new[] { "TenantId", "CompanyId", "Id" });

            migrationBuilder.CreateTable(
                name: "SalesCreditNotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SalesInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    CreditNoteNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreditNoteDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NetTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesCreditNotes", x => x.Id);
                    table.UniqueConstraint("AK_SalesCreditNotes_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_SalesCreditNotes_Totals", "\"NetTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"GrandTotal\" = \"NetTotal\" + \"TaxTotal\"");
                    table.ForeignKey(
                        name: "FK_SalesCreditNotes_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCreditNotes_JournalEntries_TenantId_CompanyId_JournalE~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCreditNotes_SalesInvoices_TenantId_CompanyId_SalesInvo~",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesInvoiceId },
                        principalTable: "SalesInvoices",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesCreditNoteLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SalesCreditNoteId = table.Column<long>(type: "bigint", nullable: false),
                    SalesInvoiceLineId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_SalesCreditNoteLines", x => x.Id);
                    table.CheckConstraint("CK_SalesCreditNoteLines_Amounts", "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"NetAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"GrossAmount\" = \"NetAmount\" + \"TaxAmount\"");
                    table.ForeignKey(
                        name: "FK_SalesCreditNoteLines_ChartOfAccounts_TenantId_CompanyId_Acc~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCreditNoteLines_SalesCreditNotes_TenantId_CompanyId_Sa~",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesCreditNoteId },
                        principalTable: "SalesCreditNotes",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesCreditNoteLines_SalesInvoiceLines_TenantId_CompanyId_S~",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesInvoiceLineId },
                        principalTable: "SalesInvoiceLines",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCreditNoteLines_TaxCodes_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNoteLines_TaxCodeId",
                table: "SalesCreditNoteLines",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNoteLines_TenantId_CompanyId_AccountId",
                table: "SalesCreditNoteLines",
                columns: new[] { "TenantId", "CompanyId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNoteLines_TenantId_CompanyId_SalesCreditNoteId",
                table: "SalesCreditNoteLines",
                columns: new[] { "TenantId", "CompanyId", "SalesCreditNoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNoteLines_TenantId_CompanyId_SalesInvoiceLineId",
                table: "SalesCreditNoteLines",
                columns: new[] { "TenantId", "CompanyId", "SalesInvoiceLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNotes_TenantId_CompanyId_CreditNoteNumber",
                table: "SalesCreditNotes",
                columns: new[] { "TenantId", "CompanyId", "CreditNoteNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNotes_TenantId_CompanyId_JournalEntryId",
                table: "SalesCreditNotes",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNotes_TenantId_CompanyId_SalesInvoiceId",
                table: "SalesCreditNotes",
                columns: new[] { "TenantId", "CompanyId", "SalesInvoiceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesCreditNoteLines");

            migrationBuilder.DropTable(
                name: "SalesCreditNotes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SalesInvoiceLines_TenantId_CompanyId_Id",
                table: "SalesInvoiceLines");
        }
    }
}
