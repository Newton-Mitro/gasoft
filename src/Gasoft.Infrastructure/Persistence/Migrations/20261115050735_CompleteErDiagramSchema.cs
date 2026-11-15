using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteErDiagramSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CostCenterId",
                table: "JournalLines",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyUsers",
                columns: table => new
                {
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyUsers", x => new { x.TenantId, x.CompanyId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CompanyUsers_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyUsers_TenantUsers_TenantId_UserId",
                        columns: x => new { x.TenantId, x.UserId },
                        principalTable: "TenantUsers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidationGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ReportingCurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidationGroups", x => x.Id);
                    table.UniqueConstraint("AK_ConsolidationGroups_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ConsolidationGroups_Currencies_ReportingCurrencyCode",
                        column: x => x.ReportingCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsolidationGroups_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CostCenters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCenters", x => x.Id);
                    table.UniqueConstraint("AK_CostCenters_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_CostCenters_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartyAddresses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PartyId = table.Column<long>(type: "bigint", nullable: false),
                    AddressType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CountryCode = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyAddresses_Parties_TenantId_PartyId",
                        columns: x => new { x.TenantId, x.PartyId },
                        principalTable: "Parties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidationGroupCompanies",
                columns: table => new
                {
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    ConsolidationGroupId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidationGroupCompanies", x => new { x.TenantId, x.ConsolidationGroupId, x.CompanyId });
                    table.ForeignKey(
                        name: "FK_ConsolidationGroupCompanies_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsolidationGroupCompanies_ConsolidationGroups_TenantId_Co~",
                        columns: x => new { x.TenantId, x.ConsolidationGroupId },
                        principalTable: "ConsolidationGroups",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidationRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConsolidationGroupId = table.Column<long>(type: "bigint", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidationRuns", x => x.Id);
                    table.UniqueConstraint("AK_ConsolidationRuns_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ConsolidationRuns_ConsolidationGroups_TenantId_Consolidatio~",
                        columns: x => new { x.TenantId, x.ConsolidationGroupId },
                        principalTable: "ConsolidationGroups",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidationJournalLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConsolidationRunId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    AccountCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidationJournalLines", x => x.Id);
                    table.CheckConstraint("CK_ConsolidationJournalLines_Amounts", "\"Debit\" >= 0 AND \"Credit\" >= 0 AND ((\"Debit\" > 0 AND \"Credit\" = 0) OR (\"Credit\" > 0 AND \"Debit\" = 0))");
                    table.ForeignKey(
                        name: "FK_ConsolidationJournalLines_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsolidationJournalLines_ConsolidationRuns_TenantId_Consol~",
                        columns: x => new { x.TenantId, x.ConsolidationRunId },
                        principalTable: "ConsolidationRuns",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_TenantId_CompanyId_CostCenterId",
                table: "JournalLines",
                columns: new[] { "TenantId", "CompanyId", "CostCenterId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyUsers_TenantId_UserId",
                table: "CompanyUsers",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationGroupCompanies_TenantId_CompanyId",
                table: "ConsolidationGroupCompanies",
                columns: new[] { "TenantId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationGroups_ReportingCurrencyCode",
                table: "ConsolidationGroups",
                column: "ReportingCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationGroups_TenantId_Code",
                table: "ConsolidationGroups",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationJournalLines_TenantId_CompanyId",
                table: "ConsolidationJournalLines",
                columns: new[] { "TenantId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationJournalLines_TenantId_ConsolidationRunId",
                table: "ConsolidationJournalLines",
                columns: new[] { "TenantId", "ConsolidationRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationRuns_TenantId_ConsolidationGroupId_PeriodEnd",
                table: "ConsolidationRuns",
                columns: new[] { "TenantId", "ConsolidationGroupId", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_TenantId_CompanyId_Code",
                table: "CostCenters",
                columns: new[] { "TenantId", "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyAddresses_TenantId_PartyId",
                table: "PartyAddresses",
                columns: new[] { "TenantId", "PartyId" });

            migrationBuilder.AddForeignKey(
                name: "FK_JournalLines_CostCenters_TenantId_CompanyId_CostCenterId",
                table: "JournalLines",
                columns: new[] { "TenantId", "CompanyId", "CostCenterId" },
                principalTable: "CostCenters",
                principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalLines_CostCenters_TenantId_CompanyId_CostCenterId",
                table: "JournalLines");

            migrationBuilder.DropTable(
                name: "CompanyUsers");

            migrationBuilder.DropTable(
                name: "ConsolidationGroupCompanies");

            migrationBuilder.DropTable(
                name: "ConsolidationJournalLines");

            migrationBuilder.DropTable(
                name: "CostCenters");

            migrationBuilder.DropTable(
                name: "PartyAddresses");

            migrationBuilder.DropTable(
                name: "ConsolidationRuns");

            migrationBuilder.DropTable(
                name: "ConsolidationGroups");

            migrationBuilder.DropIndex(
                name: "IX_JournalLines_TenantId_CompanyId_CostCenterId",
                table: "JournalLines");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "JournalLines");
        }
    }
}
