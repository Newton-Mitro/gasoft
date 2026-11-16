using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidationTranslationAndEliminations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodStart",
                table: "ConsolidationRuns",
                type: "date",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"ConsolidationRuns\" SET \"PeriodStart\" = date_trunc('month', \"PeriodEnd\")::date WHERE \"PeriodStart\" IS NULL;");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "PeriodStart",
                table: "ConsolidationRuns",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CompanyId",
                table: "ConsolidationJournalLines",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ConsolidationJournalLines",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsElimination",
                table: "ConsolidationJournalLines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTranslationAdjustment",
                table: "ConsolidationJournalLines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ConsolidationTranslationRates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConsolidationRunId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SourceCurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    TargetCurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ClosingRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    AverageRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidationTranslationRates", x => x.Id);
                    table.CheckConstraint("CK_ConsolidationTranslationRates_Rates", "\"ClosingRate\" > 0 AND \"AverageRate\" > 0");
                    table.ForeignKey(
                        name: "FK_ConsolidationTranslationRates_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsolidationTranslationRates_ConsolidationRuns_TenantId_Co~",
                        columns: x => new { x.TenantId, x.ConsolidationRunId },
                        principalTable: "ConsolidationRuns",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsolidationTranslationRates_Currencies_SourceCurrencyCode",
                        column: x => x.SourceCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsolidationTranslationRates_Currencies_TargetCurrencyCode",
                        column: x => x.TargetCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationTranslationRates_SourceCurrencyCode",
                table: "ConsolidationTranslationRates",
                column: "SourceCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationTranslationRates_TargetCurrencyCode",
                table: "ConsolidationTranslationRates",
                column: "TargetCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationTranslationRates_TenantId_CompanyId",
                table: "ConsolidationTranslationRates",
                columns: new[] { "TenantId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationTranslationRates_TenantId_ConsolidationRunId_C~",
                table: "ConsolidationTranslationRates",
                columns: new[] { "TenantId", "ConsolidationRunId", "CompanyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsolidationTranslationRates");

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                table: "ConsolidationRuns");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ConsolidationJournalLines");

            migrationBuilder.DropColumn(
                name: "IsElimination",
                table: "ConsolidationJournalLines");

            migrationBuilder.DropColumn(
                name: "IsTranslationAdjustment",
                table: "ConsolidationJournalLines");

            migrationBuilder.AlterColumn<long>(
                name: "CompanyId",
                table: "ConsolidationJournalLines",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
