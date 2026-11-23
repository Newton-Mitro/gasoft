-- Sample master data for Gasoft's PostgreSQL database.
-- Apply the EF Core migrations first, then replace these IDs with an existing
-- tenant/company pair. The script inserts missing codes and leaves existing
-- records unchanged.
--
-- To find IDs:
-- SELECT "Id", "Name" FROM "Tenants" ORDER BY "Id";
-- SELECT "Id", "TenantId", "Code", "LegalName" FROM "Companies" ORDER BY "Id";

DO $seed$
DECLARE
    target_tenant_id bigint := 1; -- Set this to the target tenant ID.
    target_company_id bigint := 1; -- Set this to a company in that tenant.
BEGIN
    IF target_tenant_id <= 0 OR target_company_id <= 0 THEN
        RAISE EXCEPTION 'Set target_tenant_id and target_company_id before running this script.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM "Tenants"
        WHERE "Id" = target_tenant_id
    ) THEN
        RAISE EXCEPTION 'Tenant % does not exist.', target_tenant_id;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM "Companies"
        WHERE "TenantId" = target_tenant_id
          AND "Id" = target_company_id
    ) THEN
        RAISE EXCEPTION 'Company % does not belong to tenant %.',
            target_company_id, target_tenant_id;
    END IF;

    INSERT INTO "Parties"
        ("TenantId", "PartyNo", "DisplayName", "IsCustomer", "IsSupplier",
         "Email", "Phone", "DefaultCurrencyCode")
    VALUES
        (target_tenant_id, 'CUST-SEED-001', 'Sample Retail Customer',
         TRUE, FALSE, 'customer@example.com', NULL, NULL),
        (target_tenant_id, 'CUST-SEED-002', 'Sample Business Customer',
         TRUE, FALSE, 'business-customer@example.com', NULL, NULL),
        (target_tenant_id, 'SUP-SEED-001', 'Sample Office Supplier',
         FALSE, TRUE, 'office-supplier@example.com', NULL, NULL),
        (target_tenant_id, 'SUP-SEED-002', 'Sample Electronics Supplier',
         FALSE, TRUE, 'electronics-supplier@example.com', NULL, NULL)
    ON CONFLICT ("TenantId", "PartyNo") DO NOTHING;

    INSERT INTO "ItemCategories" ("TenantId", "Code", "Name")
    VALUES
        (target_tenant_id, 'OFFICE', 'Office Supplies'),
        (target_tenant_id, 'ELECTRONICS', 'Electronics'),
        (target_tenant_id, 'CONSUMABLES', 'Consumables')
    ON CONFLICT ("TenantId", "Code") DO NOTHING;

    INSERT INTO "Items"
        ("TenantId", "CategoryId", "BaseUnitCode", "Sku", "Name",
         "CostingMethod", "IsActive")
    SELECT
        target_tenant_id,
        category."Id",
        seed."BaseUnitCode",
        seed."Sku",
        seed."Name",
        seed."CostingMethod",
        TRUE
    FROM (VALUES
        ('PAPER-A4', 'A4 Copy Paper', 'OFFICE', 'EA', 'FIFO'),
        ('PEN-BLUE', 'Blue Ballpoint Pen', 'OFFICE', 'EA', 'AVCO'),
        ('MOUSE-USB', 'USB Optical Mouse', 'ELECTRONICS', 'EA', 'FIFO'),
        ('WATER-BOX', 'Bottled Water (Box)', 'CONSUMABLES', 'BOX', 'AVCO')
    ) AS seed("Sku", "Name", "CategoryCode", "BaseUnitCode", "CostingMethod")
    JOIN "ItemCategories" AS category
      ON category."TenantId" = target_tenant_id
     AND category."Code" = seed."CategoryCode"
    WHERE TRUE
    ON CONFLICT ("TenantId", "Sku") DO NOTHING;

    INSERT INTO "Warehouses" ("TenantId", "CompanyId", "Code", "Name")
    VALUES (target_tenant_id, target_company_id, 'MAIN', 'Main Warehouse')
    ON CONFLICT ("TenantId", "CompanyId", "Code") DO NOTHING;

    -- Opening stock is seeded into inventory valuation only; this does not
    -- create a general-ledger journal entry. Unit costs use the company's
    -- functional currency.
    WITH stock_seed("Sku", "Quantity", "UnitCost") AS (
        VALUES
            ('PAPER-A4', 100::numeric, 55.00::numeric),
            ('PEN-BLUE', 500::numeric, 12.00::numeric),
            ('MOUSE-USB', 40::numeric, 650.00::numeric),
            ('WATER-BOX', 80::numeric, 240.00::numeric)
    ),
    inserted_movements AS (
        INSERT INTO "StockMovements"
            ("TenantId", "CompanyId", "ItemId", "WarehouseId", "MovementNumber",
             "MovementType", "MovementDate", "QuantityIn", "QuantityOut",
             "UnitCost", "TotalCost", "Revision")
        SELECT
            target_tenant_id,
            target_company_id,
            item."Id",
            warehouse."Id",
            'OPENING-SEED-' || stock_seed."Sku",
            'OpeningBalance',
            CURRENT_DATE,
            stock_seed."Quantity",
            0,
            stock_seed."UnitCost",
            stock_seed."Quantity" * stock_seed."UnitCost",
            0
        FROM stock_seed
        JOIN "Items" AS item
          ON item."TenantId" = target_tenant_id
         AND item."Sku" = stock_seed."Sku"
        JOIN "Warehouses" AS warehouse
          ON warehouse."TenantId" = target_tenant_id
         AND warehouse."CompanyId" = target_company_id
         AND warehouse."Code" = 'MAIN'
        WHERE TRUE
        ON CONFLICT ("TenantId", "CompanyId", "MovementNumber") DO NOTHING
        RETURNING
            "TenantId", "CompanyId", "Id", "ItemId", "WarehouseId",
            "QuantityIn", "UnitCost"
    )
    INSERT INTO "InventoryValuationLayers"
        ("TenantId", "CompanyId", "StockMovementId", "ItemId", "WarehouseId",
         "RemainingQuantity", "UnitCost", "Revision")
    SELECT
        "TenantId", "CompanyId", "Id", "ItemId", "WarehouseId",
        "QuantityIn", "UnitCost", 0
    FROM inserted_movements;
END;
$seed$;
