using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseOrdersAndReceiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PurchaseOrderLineId",
                table: "StockMovements",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseOrders_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_PurchaseOrders_ExchangeRate", "\"ExchangeRate\" > 0");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Parties_TenantId_SupplierId",
                        columns: x => new { x.TenantId, x.SupplierId },
                        principalTable: "Parties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Warehouses_TenantId_CompanyId_WarehouseId",
                        columns: x => new { x.TenantId, x.CompanyId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseOrderId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseOrderLines_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_PurchaseOrderLines_Quantities", "\"Quantity\" > 0 AND \"ReceivedQuantity\" >= 0 AND \"ReceivedQuantity\" <= \"Quantity\" AND \"UnitCost\" > 0");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_Items_TenantId_ItemId",
                        columns: x => new { x.TenantId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_TenantId_CompanyId_Purcha~",
                        columns: x => new { x.TenantId, x.CompanyId, x.PurchaseOrderId },
                        principalTable: "PurchaseOrders",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_CompanyId_PurchaseOrderLineId",
                table: "StockMovements",
                columns: new[] { "TenantId", "CompanyId", "PurchaseOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_TenantId_CompanyId_PurchaseOrderId",
                table: "PurchaseOrderLines",
                columns: new[] { "TenantId", "CompanyId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_TenantId_ItemId",
                table: "PurchaseOrderLines",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CurrencyCode",
                table: "PurchaseOrders",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_CompanyId_OrderNumber",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "CompanyId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_CompanyId_WarehouseId",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "CompanyId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_SupplierId",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "SupplierId" });

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_PurchaseOrderLines_TenantId_CompanyId_Purcha~",
                table: "StockMovements",
                columns: new[] { "TenantId", "CompanyId", "PurchaseOrderLineId" },
                principalTable: "PurchaseOrderLines",
                principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_PurchaseOrderLines_TenantId_CompanyId_Purcha~",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_CompanyId_PurchaseOrderLineId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderLineId",
                table: "StockMovements");
        }
    }
}
