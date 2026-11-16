using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ReceivableAccountId",
                table: "SalesCreditNotes",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerRefunds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SalesCreditNoteId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: false),
                    RefundNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RefundDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    FunctionalAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerRefunds", x => x.Id);
                    table.UniqueConstraint("AK_CustomerRefunds_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_CustomerRefunds_Amounts", "\"Amount\" > 0 AND \"FunctionalAmount\" > 0");
                    table.ForeignKey(
                        name: "FK_CustomerRefunds_JournalEntries_TenantId_CompanyId_JournalEn~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerRefunds_SalesCreditNotes_TenantId_CompanyId_SalesCr~",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesCreditNoteId },
                        principalTable: "SalesCreditNotes",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNotes_TenantId_CompanyId_ReceivableAccountId",
                table: "SalesCreditNotes",
                columns: new[] { "TenantId", "CompanyId", "ReceivableAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRefunds_TenantId_CompanyId_JournalEntryId",
                table: "CustomerRefunds",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRefunds_TenantId_CompanyId_RefundNumber",
                table: "CustomerRefunds",
                columns: new[] { "TenantId", "CompanyId", "RefundNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRefunds_TenantId_CompanyId_SalesCreditNoteId",
                table: "CustomerRefunds",
                columns: new[] { "TenantId", "CompanyId", "SalesCreditNoteId" });

            migrationBuilder.AddForeignKey(
                name: "FK_SalesCreditNotes_ChartOfAccounts_TenantId_CompanyId_Receiva~",
                table: "SalesCreditNotes",
                columns: new[] { "TenantId", "CompanyId", "ReceivableAccountId" },
                principalTable: "ChartOfAccounts",
                principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                UPDATE "SalesCreditNotes" AS note
                SET "ReceivableAccountId" = (
                    SELECT line."AccountId"
                    FROM "JournalLines" AS line
                    WHERE line."TenantId" = note."TenantId"
                      AND line."CompanyId" = note."CompanyId"
                      AND line."JournalEntryId" = note."JournalEntryId"
                      AND line."Credit" > 0
                    ORDER BY line."Id"
                    LIMIT 1)
                WHERE note."Status" = 'Posted'
                  AND note."JournalEntryId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesCreditNotes_ChartOfAccounts_TenantId_CompanyId_Receiva~",
                table: "SalesCreditNotes");

            migrationBuilder.DropTable(
                name: "CustomerRefunds");

            migrationBuilder.DropIndex(
                name: "IX_SalesCreditNotes_TenantId_CompanyId_ReceivableAccountId",
                table: "SalesCreditNotes");

            migrationBuilder.DropColumn(
                name: "ReceivableAccountId",
                table: "SalesCreditNotes");
        }
    }
}
