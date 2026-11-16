using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BankReconciliationAllocationsAndAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankReconciliationMatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    BankAccountId = table.Column<long>(type: "bigint", nullable: false),
                    BankStatementTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    JournalLineId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankReconciliationMatches", x => x.Id);
                    table.UniqueConstraint("AK_BankReconciliationMatches_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_BankReconciliationMatches_Amount", "\"Amount\" <> 0");
                    table.ForeignKey(
                        name: "FK_BankReconciliationMatches_BankAccounts_TenantId_CompanyId_B~",
                        columns: x => new { x.TenantId, x.CompanyId, x.BankAccountId },
                        principalTable: "BankAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankReconciliationMatches_BankStatementTransactions_TenantI~",
                        columns: x => new { x.TenantId, x.CompanyId, x.BankStatementTransactionId },
                        principalTable: "BankStatementTransactions",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BankReconciliationMatches_JournalLines_TenantId_CompanyId_J~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalLineId },
                        principalTable: "JournalLines",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "BankReconciliationMatches"
                    ("CompanyId", "BankAccountId", "BankStatementTransactionId", "JournalLineId", "Amount", "TenantId")
                SELECT "CompanyId", "BankAccountId", "Id", "JournalLineId", "Amount", "TenantId"
                FROM "BankStatementTransactions"
                WHERE "JournalLineId" IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_BankStatementTransactions_JournalLines_TenantId_CompanyId_J~",
                table: "BankStatementTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BankStatementTransactions_TenantId_CompanyId_JournalLineId",
                table: "BankStatementTransactions");

            migrationBuilder.DropColumn(
                name: "JournalLineId",
                table: "BankStatementTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationMatches_TenantId_CompanyId_BankAccountId",
                table: "BankReconciliationMatches",
                columns: new[] { "TenantId", "CompanyId", "BankAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationMatches_TenantId_CompanyId_BankStatementT~",
                table: "BankReconciliationMatches",
                columns: new[] { "TenantId", "CompanyId", "BankStatementTransactionId", "JournalLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationMatches_TenantId_CompanyId_JournalLineId",
                table: "BankReconciliationMatches",
                columns: new[] { "TenantId", "CompanyId", "JournalLineId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "JournalLineId",
                table: "BankStatementTransactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH complete_matches AS (
                    SELECT match."TenantId", match."CompanyId", match."BankStatementTransactionId",
                           MIN(match."JournalLineId") AS "JournalLineId"
                    FROM "BankReconciliationMatches" AS match
                    INNER JOIN "BankStatementTransactions" AS statement
                        ON statement."TenantId" = match."TenantId"
                        AND statement."CompanyId" = match."CompanyId"
                        AND statement."Id" = match."BankStatementTransactionId"
                    GROUP BY match."TenantId", match."CompanyId", match."BankStatementTransactionId", statement."Amount"
                    HAVING COUNT(*) = 1 AND SUM(match."Amount") = statement."Amount"
                ),
                unique_ledger_matches AS (
                    SELECT complete_matches.*,
                           COUNT(*) OVER (
                               PARTITION BY complete_matches."TenantId", complete_matches."CompanyId", complete_matches."JournalLineId"
                           ) AS "LedgerMatchCount"
                    FROM complete_matches
                )
                UPDATE "BankStatementTransactions" AS statement
                SET "JournalLineId" = matches."JournalLineId"
                FROM unique_ledger_matches AS matches
                WHERE statement."TenantId" = matches."TenantId"
                    AND statement."CompanyId" = matches."CompanyId"
                    AND statement."Id" = matches."BankStatementTransactionId"
                    AND matches."LedgerMatchCount" = 1;
                """);

            migrationBuilder.DropTable(
                name: "BankReconciliationMatches");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementTransactions_TenantId_CompanyId_JournalLineId",
                table: "BankStatementTransactions",
                columns: new[] { "TenantId", "CompanyId", "JournalLineId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BankStatementTransactions_JournalLines_TenantId_CompanyId_J~",
                table: "BankStatementTransactions",
                columns: new[] { "TenantId", "CompanyId", "JournalLineId" },
                principalTable: "JournalLines",
                principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
