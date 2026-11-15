using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BillingAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Parties_TenantId_Id",
                table: "Parties",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    PartyId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.UniqueConstraint("AK_Payments_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_Payments_Amounts", "\"Amount\" > 0 AND \"AllocatedAmount\" >= 0 AND \"AllocatedAmount\" <= \"Amount\"");
                    table.CheckConstraint("CK_Payments_Direction", "\"Direction\" IN ('Receive', 'Pay')");
                    table.ForeignKey(
                        name: "FK_Payments_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_JournalEntries_TenantId_CompanyId_JournalEntryId",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Parties_TenantId_PartyId",
                        columns: x => new { x.TenantId, x.PartyId },
                        principalTable: "Parties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseBills",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    BillNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    BillDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    NetTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    AllocatedTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseBills", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseBills_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_PurchaseBills_Totals", "\"NetTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"GrandTotal\" = \"NetTotal\" + \"TaxTotal\" AND \"AllocatedTotal\" >= 0 AND \"AllocatedTotal\" <= \"GrandTotal\"");
                    table.ForeignKey(
                        name: "FK_PurchaseBills_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseBills_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseBills_JournalEntries_TenantId_CompanyId_JournalEntr~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseBills_Parties_TenantId_SupplierId",
                        columns: x => new { x.TenantId, x.SupplierId },
                        principalTable: "Parties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    NetTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    AllocatedTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoices", x => x.Id);
                    table.UniqueConstraint("AK_SalesInvoices_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_SalesInvoices_Totals", "\"NetTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"GrandTotal\" = \"NetTotal\" + \"TaxTotal\" AND \"AllocatedTotal\" >= 0 AND \"AllocatedTotal\" <= \"GrandTotal\"");
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_JournalEntries_TenantId_CompanyId_JournalEntr~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Parties_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalTable: "Parties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseBillLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseBillId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_PurchaseBillLines", x => x.Id);
                    table.CheckConstraint("CK_PurchaseBillLines_Amounts", "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"NetAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"GrossAmount\" = \"NetAmount\" + \"TaxAmount\"");
                    table.ForeignKey(
                        name: "FK_PurchaseBillLines_ChartOfAccounts_TenantId_CompanyId_Accoun~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseBillLines_PurchaseBills_TenantId_CompanyId_Purchase~",
                        columns: x => new { x.TenantId, x.CompanyId, x.PurchaseBillId },
                        principalTable: "PurchaseBills",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseBillLines_TaxCodes_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentId = table.Column<long>(type: "bigint", nullable: false),
                    SalesInvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    PurchaseBillId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.CheckConstraint("CK_PaymentAllocations_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_PaymentAllocations_Target", "(\"SalesInvoiceId\" IS NOT NULL AND \"PurchaseBillId\" IS NULL) OR (\"SalesInvoiceId\" IS NULL AND \"PurchaseBillId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Payments_TenantId_CompanyId_PaymentId",
                        columns: x => new { x.TenantId, x.CompanyId, x.PaymentId },
                        principalTable: "Payments",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_PurchaseBills_TenantId_CompanyId_Purchas~",
                        columns: x => new { x.TenantId, x.CompanyId, x.PurchaseBillId },
                        principalTable: "PurchaseBills",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_SalesInvoices_TenantId_CompanyId_SalesIn~",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesInvoiceId },
                        principalTable: "SalesInvoices",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SalesInvoiceId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_SalesInvoiceLines", x => x.Id);
                    table.CheckConstraint("CK_SalesInvoiceLines_Amounts", "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"NetAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"GrossAmount\" = \"NetAmount\" + \"TaxAmount\"");
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_ChartOfAccounts_TenantId_CompanyId_Accoun~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_SalesInvoices_TenantId_CompanyId_SalesInv~",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesInvoiceId },
                        principalTable: "SalesInvoices",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_TaxCodes_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_TenantId_CompanyId_PaymentId",
                table: "PaymentAllocations",
                columns: new[] { "TenantId", "CompanyId", "PaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_TenantId_CompanyId_PurchaseBillId",
                table: "PaymentAllocations",
                columns: new[] { "TenantId", "CompanyId", "PurchaseBillId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_TenantId_CompanyId_SalesInvoiceId",
                table: "PaymentAllocations",
                columns: new[] { "TenantId", "CompanyId", "SalesInvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CurrencyCode",
                table: "Payments",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_CompanyId_JournalEntryId",
                table: "Payments",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_CompanyId_PaymentNumber",
                table: "Payments",
                columns: new[] { "TenantId", "CompanyId", "PaymentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_PartyId",
                table: "Payments",
                columns: new[] { "TenantId", "PartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseBillLines_TaxCodeId",
                table: "PurchaseBillLines",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseBillLines_TenantId_CompanyId_AccountId",
                table: "PurchaseBillLines",
                columns: new[] { "TenantId", "CompanyId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseBillLines_TenantId_CompanyId_PurchaseBillId",
                table: "PurchaseBillLines",
                columns: new[] { "TenantId", "CompanyId", "PurchaseBillId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseBills_CurrencyCode",
                table: "PurchaseBills",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseBills_TenantId_CompanyId_BillNumber",
                table: "PurchaseBills",
                columns: new[] { "TenantId", "CompanyId", "BillNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseBills_TenantId_CompanyId_JournalEntryId",
                table: "PurchaseBills",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseBills_TenantId_SupplierId",
                table: "PurchaseBills",
                columns: new[] { "TenantId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_TaxCodeId",
                table: "SalesInvoiceLines",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_TenantId_CompanyId_AccountId",
                table: "SalesInvoiceLines",
                columns: new[] { "TenantId", "CompanyId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_TenantId_CompanyId_SalesInvoiceId",
                table: "SalesInvoiceLines",
                columns: new[] { "TenantId", "CompanyId", "SalesInvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_CurrencyCode",
                table: "SalesInvoices",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_TenantId_CompanyId_InvoiceNumber",
                table: "SalesInvoices",
                columns: new[] { "TenantId", "CompanyId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_TenantId_CompanyId_JournalEntryId",
                table: "SalesInvoices",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_TenantId_CustomerId",
                table: "SalesInvoices",
                columns: new[] { "TenantId", "CustomerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentAllocations");

            migrationBuilder.DropTable(
                name: "PurchaseBillLines");

            migrationBuilder.DropTable(
                name: "SalesInvoiceLines");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PurchaseBills");

            migrationBuilder.DropTable(
                name: "SalesInvoices");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Parties_TenantId_Id",
                table: "Parties");
        }
    }
}
