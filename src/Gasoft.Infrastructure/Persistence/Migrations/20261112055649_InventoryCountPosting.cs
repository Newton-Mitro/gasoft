using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InventoryCountPosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryCountLines_Quantities",
                table: "InventoryCountLines");

            migrationBuilder.AddColumn<long>(
                name: "JournalEntryId",
                table: "InventoryCounts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "InventoryCounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SnapshotMovementId",
                table: "InventoryCounts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "AdjustmentUnitCost",
                table: "InventoryCountLines",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "InventoryCountLines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SystemValue",
                table: "InventoryCountLines",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCounts_TenantId_CompanyId_JournalEntryId",
                table: "InventoryCounts",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryCountLines_Quantities",
                table: "InventoryCountLines",
                sql: "\"SystemQuantity\" >= 0 AND \"SystemValue\" >= 0 AND \"CountedQuantity\" >= 0 AND (\"AdjustmentUnitCost\" IS NULL OR \"AdjustmentUnitCost\" > 0)");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryCounts_JournalEntries_TenantId_CompanyId_JournalEn~",
                table: "InventoryCounts",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" },
                principalTable: "JournalEntries",
                principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryCounts_JournalEntries_TenantId_CompanyId_JournalEn~",
                table: "InventoryCounts");

            migrationBuilder.DropIndex(
                name: "IX_InventoryCounts_TenantId_CompanyId_JournalEntryId",
                table: "InventoryCounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryCountLines_Quantities",
                table: "InventoryCountLines");

            migrationBuilder.DropColumn(
                name: "JournalEntryId",
                table: "InventoryCounts");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "InventoryCounts");

            migrationBuilder.DropColumn(
                name: "SnapshotMovementId",
                table: "InventoryCounts");

            migrationBuilder.DropColumn(
                name: "AdjustmentUnitCost",
                table: "InventoryCountLines");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "InventoryCountLines");

            migrationBuilder.DropColumn(
                name: "SystemValue",
                table: "InventoryCountLines");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryCountLines_Quantities",
                table: "InventoryCountLines",
                sql: "\"SystemQuantity\" >= 0 AND \"CountedQuantity\" >= 0");
        }
    }
}
