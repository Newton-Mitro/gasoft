using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PosSettings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    JournalId = table.Column<long>(type: "bigint", nullable: false),
                    AccountingPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    RevenueAccountId = table.Column<long>(type: "bigint", nullable: false),
                    ReceivableAccountId = table.Column<long>(type: "bigint", nullable: false),
                    SettlementAccountId = table.Column<long>(type: "bigint", nullable: false),
                    TaxCodeId = table.Column<long>(type: "bigint", nullable: true),
                    TaxAccountId = table.Column<long>(type: "bigint", nullable: true),
                    InventoryAccountId = table.Column<long>(type: "bigint", nullable: false),
                    CostOfGoodsSoldAccountId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosSettings_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PosSettings_TenantId_CompanyId",
                table: "PosSettings",
                columns: new[] { "TenantId", "CompanyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PosSettings");
        }
    }
}
