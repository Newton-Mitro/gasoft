using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InventoryFoundations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCategories", x => x.Id);
                    table.UniqueConstraint("AK_ItemCategories_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ItemCategories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitsOfMeasure",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DecimalPlaces = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitsOfMeasure", x => x.Code);
                    table.CheckConstraint("CK_UnitsOfMeasure_DecimalPlaces", "\"DecimalPlaces\" <= 6");
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.UniqueConstraint("AK_Warehouses_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_Warehouses_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    BaseUnitCode = table.Column<string>(type: "character varying(12)", nullable: false),
                    Sku = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CostingMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.UniqueConstraint("AK_Items_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Items_ItemCategories_TenantId_CategoryId",
                        columns: x => new { x.TenantId, x.CategoryId },
                        principalTable: "ItemCategories",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Items_UnitsOfMeasure_BaseUnitCode",
                        column: x => x.BaseUnitCode,
                        principalTable: "UnitsOfMeasure",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    CountNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CountDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCounts", x => x.Id);
                    table.UniqueConstraint("AK_InventoryCounts_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_InventoryCounts_Warehouses_TenantId_CompanyId_WarehouseId",
                        columns: x => new { x.TenantId, x.CompanyId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseLocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseLocations", x => x.Id);
                    table.UniqueConstraint("AK_WarehouseLocations_TenantId_CompanyId_WarehouseId_Id", x => new { x.TenantId, x.CompanyId, x.WarehouseId, x.Id });
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_Warehouses_TenantId_CompanyId_WarehouseId",
                        columns: x => new { x.TenantId, x.CompanyId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCountLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    InventoryCountId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    SystemQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCountLines", x => x.Id);
                    table.CheckConstraint("CK_InventoryCountLines_Quantities", "\"SystemQuantity\" >= 0 AND \"CountedQuantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_InventoryCountLines_InventoryCounts_TenantId_CompanyId_Inve~",
                        columns: x => new { x.TenantId, x.CompanyId, x.InventoryCountId },
                        principalTable: "InventoryCounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryCountLines_Items_TenantId_ItemId",
                        columns: x => new { x.TenantId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseLocationId = table.Column<long>(type: "bigint", nullable: true),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    MovementNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MovementType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MovementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    QuantityIn = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    QuantityOut = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.UniqueConstraint("AK_StockMovements_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_StockMovements_Cost", "\"UnitCost\" >= 0 AND \"TotalCost\" >= 0");
                    table.CheckConstraint("CK_StockMovements_Quantities", "\"QuantityIn\" >= 0 AND \"QuantityOut\" >= 0 AND ((\"QuantityIn\" > 0 AND \"QuantityOut\" = 0) OR (\"QuantityOut\" > 0 AND \"QuantityIn\" = 0))");
                    table.ForeignKey(
                        name: "FK_StockMovements_Items_TenantId_ItemId",
                        columns: x => new { x.TenantId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_JournalEntries_TenantId_CompanyId_JournalEnt~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_WarehouseLocations_TenantId_CompanyId_Wareho~",
                        columns: x => new { x.TenantId, x.CompanyId, x.WarehouseId, x.WarehouseLocationId },
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "TenantId", "CompanyId", "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Warehouses_TenantId_CompanyId_WarehouseId",
                        columns: x => new { x.TenantId, x.CompanyId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryValuationLayers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    StockMovementId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryValuationLayers", x => x.Id);
                    table.CheckConstraint("CK_InventoryValuationLayers_Amounts", "\"RemainingQuantity\" >= 0 AND \"UnitCost\" >= 0");
                    table.ForeignKey(
                        name: "FK_InventoryValuationLayers_Items_TenantId_ItemId",
                        columns: x => new { x.TenantId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryValuationLayers_StockMovements_TenantId_CompanyId_~",
                        columns: x => new { x.TenantId, x.CompanyId, x.StockMovementId },
                        principalTable: "StockMovements",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryValuationLayers_Warehouses_TenantId_CompanyId_Ware~",
                        columns: x => new { x.TenantId, x.CompanyId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "UnitsOfMeasure",
                columns: new[] { "Code", "DecimalPlaces", "Name" },
                values: new object[,]
                {
                    { "BOX", (byte)0, "Box" },
                    { "EA", (byte)0, "Each" },
                    { "HOUR", (byte)2, "Hour" },
                    { "KG", (byte)3, "Kilogram" },
                    { "L", (byte)3, "Litre" },
                    { "M", (byte)3, "Metre" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountLines_TenantId_CompanyId_InventoryCountId",
                table: "InventoryCountLines",
                columns: new[] { "TenantId", "CompanyId", "InventoryCountId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountLines_TenantId_ItemId",
                table: "InventoryCountLines",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCounts_TenantId_CompanyId_CountNumber",
                table: "InventoryCounts",
                columns: new[] { "TenantId", "CompanyId", "CountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCounts_TenantId_CompanyId_WarehouseId",
                table: "InventoryCounts",
                columns: new[] { "TenantId", "CompanyId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryValuationLayers_TenantId_CompanyId_StockMovementId",
                table: "InventoryValuationLayers",
                columns: new[] { "TenantId", "CompanyId", "StockMovementId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryValuationLayers_TenantId_CompanyId_WarehouseId",
                table: "InventoryValuationLayers",
                columns: new[] { "TenantId", "CompanyId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryValuationLayers_TenantId_ItemId",
                table: "InventoryValuationLayers",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_TenantId_Code",
                table: "ItemCategories",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_BaseUnitCode",
                table: "Items",
                column: "BaseUnitCode");

            migrationBuilder.CreateIndex(
                name: "IX_Items_TenantId_CategoryId",
                table: "Items",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_TenantId_Sku",
                table: "Items",
                columns: new[] { "TenantId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_CompanyId_JournalEntryId",
                table: "StockMovements",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_CompanyId_MovementNumber",
                table: "StockMovements",
                columns: new[] { "TenantId", "CompanyId", "MovementNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_CompanyId_WarehouseId_WarehouseLoca~",
                table: "StockMovements",
                columns: new[] { "TenantId", "CompanyId", "WarehouseId", "WarehouseLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_ItemId",
                table: "StockMovements",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_TenantId_CompanyId_WarehouseId_Code",
                table: "WarehouseLocations",
                columns: new[] { "TenantId", "CompanyId", "WarehouseId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantId_CompanyId_Code",
                table: "Warehouses",
                columns: new[] { "TenantId", "CompanyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryCountLines");

            migrationBuilder.DropTable(
                name: "InventoryValuationLayers");

            migrationBuilder.DropTable(
                name: "InventoryCounts");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "WarehouseLocations");

            migrationBuilder.DropTable(
                name: "ItemCategories");

            migrationBuilder.DropTable(
                name: "UnitsOfMeasure");

            migrationBuilder.DropTable(
                name: "Warehouses");
        }
    }
}
