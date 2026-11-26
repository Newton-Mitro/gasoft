using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeparateSalesInvoiceCreditTotals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "SalesInvoices" AS invoice
                        LEFT JOIN (
                            SELECT "TenantId", "CompanyId", "SalesInvoiceId", SUM("GrandTotal") AS "Total"
                            FROM "SalesCreditNotes"
                            WHERE "Status" = 'Posted'
                            GROUP BY "TenantId", "CompanyId", "SalesInvoiceId"
                        ) AS credits
                          ON credits."TenantId" = invoice."TenantId"
                         AND credits."CompanyId" = invoice."CompanyId"
                         AND credits."SalesInvoiceId" = invoice."Id"
                        WHERE COALESCE(credits."Total", 0) > invoice."AllocatedTotal"
                    ) THEN
                        RAISE EXCEPTION 'Cannot separate invoice totals because posted credit notes exceed the allocated total.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_SalesInvoices_Totals",
                table: "SalesInvoices");

            migrationBuilder.AddColumn<decimal>(
                name: "CreditedTotal",
                table: "SalesInvoices",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedTotal",
                table: "SalesInvoices",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                WITH credit_totals AS (
                    SELECT "TenantId", "CompanyId", "SalesInvoiceId", SUM("GrandTotal") AS "Total"
                    FROM "SalesCreditNotes"
                    WHERE "Status" = 'Posted'
                    GROUP BY "TenantId", "CompanyId", "SalesInvoiceId"
                ),
                refund_totals AS (
                    SELECT note."TenantId", note."CompanyId", note."SalesInvoiceId", SUM(refund."Amount") AS "Total"
                    FROM "CustomerRefunds" AS refund
                    JOIN "SalesCreditNotes" AS note
                      ON note."TenantId" = refund."TenantId"
                     AND note."CompanyId" = refund."CompanyId"
                     AND note."Id" = refund."SalesCreditNoteId"
                    WHERE note."Status" = 'Posted'
                      AND refund."Status" = 'Posted'
                    GROUP BY note."TenantId", note."CompanyId", note."SalesInvoiceId"
                ),
                invoice_totals AS (
                    SELECT invoice."TenantId", invoice."CompanyId", invoice."Id",
                           COALESCE(credits."Total", 0) AS "CreditedTotal",
                           COALESCE(refunds."Total", 0) AS "RefundedTotal"
                    FROM "SalesInvoices" AS invoice
                    LEFT JOIN credit_totals AS credits
                      ON credits."TenantId" = invoice."TenantId"
                     AND credits."CompanyId" = invoice."CompanyId"
                     AND credits."SalesInvoiceId" = invoice."Id"
                    LEFT JOIN refund_totals AS refunds
                      ON refunds."TenantId" = invoice."TenantId"
                     AND refunds."CompanyId" = invoice."CompanyId"
                     AND refunds."SalesInvoiceId" = invoice."Id"
                )
                UPDATE "SalesInvoices" AS invoice
                SET "CreditedTotal" = totals."CreditedTotal",
                    "RefundedTotal" = totals."RefundedTotal",
                    "AllocatedTotal" = invoice."AllocatedTotal" - totals."CreditedTotal"
                FROM invoice_totals AS totals
                WHERE totals."TenantId" = invoice."TenantId"
                  AND totals."CompanyId" = invoice."CompanyId"
                  AND totals."Id" = invoice."Id";
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_SalesInvoices_Totals",
                table: "SalesInvoices",
                sql: "\"NetTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"GrandTotal\" = \"NetTotal\" + \"TaxTotal\" AND \"AllocatedTotal\" >= 0 AND \"AllocatedTotal\" <= \"GrandTotal\" AND \"CreditedTotal\" >= 0 AND \"CreditedTotal\" <= \"GrandTotal\" AND \"RefundedTotal\" >= 0 AND \"RefundedTotal\" <= \"CreditedTotal\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "SalesInvoices"
                        WHERE "AllocatedTotal" + "CreditedTotal" > "GrandTotal"
                    ) THEN
                        RAISE EXCEPTION 'Cannot revert invoice credit totals because some invoices have credits exceeding their remaining balance.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_SalesInvoices_Totals",
                table: "SalesInvoices");

            migrationBuilder.Sql("""
                UPDATE "SalesInvoices"
                SET "AllocatedTotal" = "AllocatedTotal" + "CreditedTotal";
                """);

            migrationBuilder.DropColumn(
                name: "CreditedTotal",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "RefundedTotal",
                table: "SalesInvoices");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SalesInvoices_Totals",
                table: "SalesInvoices",
                sql: "\"NetTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"GrandTotal\" = \"NetTotal\" + \"TaxTotal\" AND \"AllocatedTotal\" >= 0 AND \"AllocatedTotal\" <= \"GrandTotal\"");
        }
    }
}
