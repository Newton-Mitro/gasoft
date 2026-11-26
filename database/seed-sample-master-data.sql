-- Sample master data for Gasoft's PostgreSQL database.
-- Apply the EF Core migrations first, then replace these IDs with an existing
-- tenant/company pair. Existing records are preserved except NULL company
-- prices, which are filled, and a prior stock-only draft journal, which is
-- upgraded to the full sample opening journal.
-- For a July-June fiscal year, run create-july-june-fiscal-year-periods.sql
-- first if no open accounting period contains today's date.
-- Selling prices are illustrative demo values in the company's functional
-- currency; review them before using the items for live sales.
-- Illustrative opening account balances are also added as a draft General
-- journal for review; replace them with actual balances before live use.
--
-- To find IDs:
-- SELECT "Id", "Name" FROM "Tenants" ORDER BY "Id";
-- SELECT "Id", "TenantId", "Code", "LegalName" FROM "Companies" ORDER BY "Id";

DO $seed$
DECLARE
    target_tenant_id bigint := 1; -- Set this to the target tenant ID.
    target_company_id bigint := 1; -- Set this to a company in that tenant.
    opening_movement_count integer;
    opening_stock_value numeric(20, 6);
    opening_equity_value numeric(20, 6);
    opening_total_debit numeric(20, 6);
    current_period_id bigint;
    general_journal_id bigint;
    functional_currency_code character(3);
    opening_journal_id bigint;
    existing_opening_journal_count integer;
    existing_opening_journal_description text;
    existing_opening_journal_status text;
    opening_account_count integer;
    legacy_journal_matches boolean;
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

    -- Seed illustrative company-specific selling prices. Fill prior NULL
    -- placeholders, but preserve any price already configured in Inventory.
    INSERT INTO "CompanyItemPrices"
        ("TenantId", "CompanyId", "ItemId", "UnitSellingPrice", "CurrencyCode")
    SELECT
        target_tenant_id,
        target_company_id,
        item."Id",
        seed."UnitSellingPrice",
        company."FunctionalCurrencyCode"
    FROM "Companies" AS company
    JOIN (VALUES
        ('PAPER-A4', 75.00::numeric),
        ('PEN-BLUE', 20.00::numeric),
        ('MOUSE-USB', 900.00::numeric),
        ('WATER-BOX', 300.00::numeric)
    ) AS seed("Sku", "UnitSellingPrice")
      ON TRUE
    JOIN "Items" AS item
      ON item."TenantId" = target_tenant_id
     AND item."Sku" = seed."Sku"
    WHERE company."TenantId" = target_tenant_id
      AND company."Id" = target_company_id
    ON CONFLICT ("TenantId", "CompanyId", "ItemId") DO UPDATE
    SET "UnitSellingPrice" = EXCLUDED."UnitSellingPrice",
        "CurrencyCode" = EXCLUDED."CurrencyCode"
    WHERE "CompanyItemPrices"."UnitSellingPrice" IS NULL;

    INSERT INTO "Warehouses" ("TenantId", "CompanyId", "Code", "Name")
    VALUES (target_tenant_id, target_company_id, 'MAIN', 'Main Warehouse')
    ON CONFLICT ("TenantId", "CompanyId", "Code") DO NOTHING;

    -- Seed the opening stock valuation. Unit costs use the company's
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

    -- Add an illustrative opening trial balance as a draft. Opening balance
    -- equity is calculated as the offset; replace the demo amounts before live use.
    SELECT COUNT(*)::integer
    INTO existing_opening_journal_count
    FROM "JournalEntries"
    WHERE "TenantId" = target_tenant_id
      AND "CompanyId" = target_company_id
      AND "Description" IN (
          'Opening balances for sample master data',
          'Opening stock balance for sample master data'
      );

    IF existing_opening_journal_count > 1 THEN
        RAISE EXCEPTION 'Multiple sample opening-balance journals already exist; resolve them before reseeding.';
    END IF;

    IF existing_opening_journal_count = 1 THEN
        SELECT "Id", "Description", "Status"
        INTO opening_journal_id, existing_opening_journal_description, existing_opening_journal_status
        FROM "JournalEntries"
        WHERE "TenantId" = target_tenant_id
          AND "CompanyId" = target_company_id
          AND "Description" IN (
              'Opening balances for sample master data',
              'Opening stock balance for sample master data'
          );
    END IF;

    IF existing_opening_journal_description = 'Opening stock balance for sample master data'
       AND existing_opening_journal_status <> 'Draft' THEN
        RAISE EXCEPTION 'The earlier stock-only opening journal is already posted; reverse it before applying full sample opening balances.';
    END IF;

    IF existing_opening_journal_description IS DISTINCT FROM 'Opening balances for sample master data' THEN
        SELECT COUNT(*)::integer, COALESCE(SUM("TotalCost"), 0)
        INTO opening_movement_count, opening_stock_value
        FROM "StockMovements"
        WHERE "TenantId" = target_tenant_id
          AND "CompanyId" = target_company_id
          AND "MovementType" = 'OpeningBalance'
          AND "MovementNumber" IN (
              'OPENING-SEED-PAPER-A4',
              'OPENING-SEED-PEN-BLUE',
              'OPENING-SEED-MOUSE-USB',
              'OPENING-SEED-WATER-BOX'
          );

        IF opening_movement_count <> 4 OR opening_stock_value <= 0 THEN
            RAISE EXCEPTION 'Expected four positive opening stock movements; found % movements worth %.',
                opening_movement_count, opening_stock_value;
        END IF;

        SELECT company."FunctionalCurrencyCode"
        INTO functional_currency_code
        FROM "Companies" AS company
        WHERE company."TenantId" = target_tenant_id
          AND company."Id" = target_company_id;

        SELECT period."Id"
        INTO current_period_id
        FROM "AccountingPeriods" AS period
        WHERE period."TenantId" = target_tenant_id
          AND period."CompanyId" = target_company_id
          AND period."Status" = 'Open'
          AND period."StartsOn" <= CURRENT_DATE
          AND period."EndsOn" >= CURRENT_DATE
          AND EXISTS (
              SELECT 1
              FROM "FiscalYears" AS fiscal_year
              WHERE fiscal_year."TenantId" = target_tenant_id
                AND fiscal_year."CompanyId" = target_company_id
                AND fiscal_year."Id" = period."FiscalYearId"
                AND fiscal_year."Status" = 'Open'
          )
        ORDER BY period."StartsOn"
        LIMIT 1;

        IF current_period_id IS NULL THEN
            RAISE EXCEPTION 'Create an open accounting period containing today before seeding opening balances.';
        END IF;

        SELECT journal."Id"
        INTO general_journal_id
        FROM "Journals" AS journal
        WHERE journal."TenantId" = target_tenant_id
          AND journal."CompanyId" = target_company_id
          AND journal."Code" = 'GEN'
          AND journal."IsActive";

        IF general_journal_id IS NULL THEN
            RAISE EXCEPTION 'An active General journal (GEN) is required to seed opening balances.';
        END IF;

        SELECT COUNT(*)::integer
        INTO opening_account_count
        FROM (VALUES
            ('1000'), ('1100'), ('1200'), ('1400'),
            ('1490'), ('2000'), ('2200'), ('3000')
        ) AS required("Code")
        JOIN "ChartOfAccounts" AS account
          ON account."TenantId" = target_tenant_id
         AND account."CompanyId" = target_company_id
         AND account."Code" = required."Code"
         AND account."IsActive"
         AND account."AllowPosting";

        IF opening_account_count <> 8 THEN
            RAISE EXCEPTION 'Active, postable accounts 1000, 1100, 1200, 1400, 1490, 2000, 2200, and 3000 are required.';
        END IF;

        opening_total_debit := 100000 + 25000 + opening_stock_value + 50000;
        opening_equity_value := opening_total_debit - 5000 - 20000 - 30000;

        IF existing_opening_journal_count = 1 THEN
            SELECT
                COUNT(*) = 2
                AND COUNT(*) FILTER (
                    WHERE account."Code" = '1200'
                      AND line."Debit" = opening_stock_value
                      AND line."Credit" = 0
                      AND line."FunctionalDebit" = opening_stock_value
                      AND line."FunctionalCredit" = 0
                ) = 1
                AND COUNT(*) FILTER (
                    WHERE account."Code" = '3000'
                      AND line."Debit" = 0
                      AND line."Credit" = opening_stock_value
                      AND line."FunctionalDebit" = 0
                      AND line."FunctionalCredit" = opening_stock_value
                ) = 1
            INTO legacy_journal_matches
            FROM "JournalLines" AS line
            JOIN "ChartOfAccounts" AS account
              ON account."TenantId" = line."TenantId"
             AND account."CompanyId" = line."CompanyId"
             AND account."Id" = line."AccountId"
            WHERE line."TenantId" = target_tenant_id
              AND line."CompanyId" = target_company_id
              AND line."JournalEntryId" = opening_journal_id;

            IF NOT legacy_journal_matches THEN
                RAISE EXCEPTION 'The earlier draft opening journal was changed; review it manually before reseeding.';
            END IF;

            UPDATE "JournalEntries"
            SET "AccountingPeriodId" = current_period_id,
                "EntryDate" = CURRENT_DATE,
                "Description" = 'Opening balances for sample master data',
                "TotalDebit" = opening_total_debit,
                "TotalCredit" = opening_total_debit,
                "Revision" = "Revision" + 1
            WHERE "Id" = opening_journal_id
              AND "TenantId" = target_tenant_id
              AND "CompanyId" = target_company_id;

            DELETE FROM "JournalLines"
            WHERE "TenantId" = target_tenant_id
              AND "CompanyId" = target_company_id
              AND "JournalEntryId" = opening_journal_id;
        ELSE
            INSERT INTO "JournalEntries"
                ("TenantId", "CompanyId", "JournalId", "AccountingPeriodId",
                 "EntryDate", "Description", "Status", "TotalDebit", "TotalCredit", "Revision")
            VALUES
                (target_tenant_id, target_company_id, general_journal_id, current_period_id,
                 CURRENT_DATE, 'Opening balances for sample master data', 'Draft',
                 opening_total_debit, opening_total_debit, 0)
            RETURNING "Id" INTO opening_journal_id;
        END IF;

        INSERT INTO "JournalLines"
            ("TenantId", "CompanyId", "JournalEntryId", "AccountId", "CostCenterId",
             "TransactionCurrencyCode", "Debit", "Credit", "FunctionalDebit",
             "FunctionalCredit", "ExchangeRate", "Description")
        SELECT
            target_tenant_id,
            target_company_id,
            opening_journal_id,
            account."Id",
            NULL::bigint,
            functional_currency_code,
            sample."Debit",
            sample."Credit",
            sample."Debit",
            sample."Credit",
            1,
            sample."Description"
        FROM (VALUES
            ('1000', 100000.00::numeric, 0::numeric, 'Opening cash'),
            ('1100', 25000.00::numeric, 0::numeric, 'Opening receivables'),
            ('1200', opening_stock_value, 0::numeric, 'Opening inventory'),
            ('1400', 50000.00::numeric, 0::numeric, 'Opening property and equipment'),
            ('1490', 0::numeric, 5000.00::numeric, 'Opening accumulated depreciation'),
            ('2000', 0::numeric, 20000.00::numeric, 'Opening payables'),
            ('2200', 0::numeric, 30000.00::numeric, 'Opening loans'),
            ('3000', 0::numeric, opening_equity_value, 'Opening balance equity')
        ) AS sample("AccountCode", "Debit", "Credit", "Description")
        JOIN "ChartOfAccounts" AS account
          ON account."TenantId" = target_tenant_id
         AND account."CompanyId" = target_company_id
         AND account."Code" = sample."AccountCode"
         AND account."IsActive"
         AND account."AllowPosting";
    END IF;
END;
$seed$;
