using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixedAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FixedAssets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    AssetNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InServiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AcquisitionCost = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ResidualValue = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    UsefulLifeMonths = table.Column<int>(type: "integer", nullable: false),
                    AssetAccountId = table.Column<long>(type: "bigint", nullable: false),
                    AccumulatedDepreciationAccountId = table.Column<long>(type: "bigint", nullable: false),
                    DepreciationExpenseAccountId = table.Column<long>(type: "bigint", nullable: false),
                    AcquisitionJournalEntryId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DisposalDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DisposalProceeds = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    DisposalJournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssets", x => x.Id);
                    table.UniqueConstraint("AK_FixedAssets_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_FixedAssets_Amounts", "\"AcquisitionCost\" > 0 AND \"ResidualValue\" >= 0 AND \"ResidualValue\" <= \"AcquisitionCost\" AND \"UsefulLifeMonths\" > 0");
                    table.ForeignKey(
                        name: "FK_FixedAssets_ChartOfAccounts_TenantId_CompanyId_AccumulatedD~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccumulatedDepreciationAccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_ChartOfAccounts_TenantId_CompanyId_AssetAccount~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AssetAccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_ChartOfAccounts_TenantId_CompanyId_Depreciation~",
                        columns: x => new { x.TenantId, x.CompanyId, x.DepreciationExpenseAccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_JournalEntries_TenantId_CompanyId_AcquisitionJo~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AcquisitionJournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_JournalEntries_TenantId_CompanyId_DisposalJourn~",
                        columns: x => new { x.TenantId, x.CompanyId, x.DisposalJournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FixedAssetDepreciations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    FixedAssetId = table.Column<long>(type: "bigint", nullable: false),
                    AccountingPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: false),
                    DepreciationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssetDepreciations", x => x.Id);
                    table.CheckConstraint("CK_FixedAssetDepreciations_Amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_FixedAssetDepreciations_AccountingPeriods_TenantId_CompanyI~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssetDepreciations_FixedAssets_TenantId_CompanyId_Fixe~",
                        columns: x => new { x.TenantId, x.CompanyId, x.FixedAssetId },
                        principalTable: "FixedAssets",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FixedAssetDepreciations_JournalEntries_TenantId_CompanyId_J~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_TenantId_CompanyId_AccountingPeriod~",
                table: "FixedAssetDepreciations",
                columns: new[] { "TenantId", "CompanyId", "AccountingPeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_TenantId_CompanyId_FixedAssetId_Acc~",
                table: "FixedAssetDepreciations",
                columns: new[] { "TenantId", "CompanyId", "FixedAssetId", "AccountingPeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_TenantId_CompanyId_JournalEntryId",
                table: "FixedAssetDepreciations",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_TenantId_CompanyId_AccumulatedDepreciationAccou~",
                table: "FixedAssets",
                columns: new[] { "TenantId", "CompanyId", "AccumulatedDepreciationAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_TenantId_CompanyId_AcquisitionJournalEntryId",
                table: "FixedAssets",
                columns: new[] { "TenantId", "CompanyId", "AcquisitionJournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_TenantId_CompanyId_AssetAccountId",
                table: "FixedAssets",
                columns: new[] { "TenantId", "CompanyId", "AssetAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_TenantId_CompanyId_AssetNumber",
                table: "FixedAssets",
                columns: new[] { "TenantId", "CompanyId", "AssetNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_TenantId_CompanyId_DepreciationExpenseAccountId",
                table: "FixedAssets",
                columns: new[] { "TenantId", "CompanyId", "DepreciationExpenseAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_TenantId_CompanyId_DisposalJournalEntryId",
                table: "FixedAssets",
                columns: new[] { "TenantId", "CompanyId", "DisposalJournalEntryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FixedAssetDepreciations");

            migrationBuilder.DropTable(
                name: "FixedAssets");
        }
    }
}
