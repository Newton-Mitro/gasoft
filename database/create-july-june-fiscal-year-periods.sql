-- Creates the July 1-June 30 fiscal year containing the database's current date
-- and its monthly accounting periods. Change these IDs to the target company.
-- Existing closed or non-monthly overlapping periods are not changed.
DO $create_fiscal_calendar$
DECLARE
    target_tenant_id bigint := 1;
    target_company_id bigint := 1;
    fiscal_year_start_year integer;
    fiscal_year_start date;
    fiscal_year_end date;
    overlapping_year_count integer;
    fiscal_year_id bigint;
    existing_year_start date;
    existing_year_end date;
    existing_year_status text;
    period_start date;
    period_end date;
    overlapping_period_count integer;
    existing_period_start date;
    existing_period_end date;
    existing_period_status text;
BEGIN
    IF target_tenant_id IS NULL OR target_tenant_id <= 0
       OR target_company_id IS NULL OR target_company_id <= 0 THEN
        RAISE EXCEPTION 'Set target_tenant_id and target_company_id to valid IDs.';
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

    fiscal_year_start_year := EXTRACT(YEAR FROM CURRENT_DATE)::integer;
    IF EXTRACT(MONTH FROM CURRENT_DATE)::integer < 7 THEN
        fiscal_year_start_year := fiscal_year_start_year - 1;
    END IF;

    fiscal_year_start := make_date(fiscal_year_start_year, 7, 1);
    fiscal_year_end := make_date(fiscal_year_start_year + 1, 6, 30);

    SELECT COUNT(*)::integer
    INTO overlapping_year_count
    FROM "FiscalYears"
    WHERE "TenantId" = target_tenant_id
      AND "CompanyId" = target_company_id
      AND "StartsOn" <= fiscal_year_end
      AND "EndsOn" >= fiscal_year_start;

    IF overlapping_year_count > 1 THEN
        RAISE EXCEPTION 'Multiple fiscal years overlap the July-June year % to %.',
            fiscal_year_start, fiscal_year_end;
    ELSIF overlapping_year_count = 1 THEN
        SELECT "Id", "StartsOn", "EndsOn", "Status"
        INTO fiscal_year_id, existing_year_start, existing_year_end, existing_year_status
        FROM "FiscalYears"
        WHERE "TenantId" = target_tenant_id
          AND "CompanyId" = target_company_id
          AND "StartsOn" <= fiscal_year_end
          AND "EndsOn" >= fiscal_year_start;

        IF existing_year_start <> fiscal_year_start
           OR existing_year_end <> fiscal_year_end THEN
            RAISE EXCEPTION 'An overlapping fiscal year already exists for company %; resolve it before creating the July-June year.',
                target_company_id;
        END IF;

        IF existing_year_status <> 'Open' THEN
            RAISE EXCEPTION 'Fiscal year % to % is closed; reopen it in the application before seeding opening balances.',
                fiscal_year_start, fiscal_year_end;
        END IF;
    ELSE
        INSERT INTO "FiscalYears"
            ("TenantId", "CompanyId", "StartsOn", "EndsOn", "Status")
        VALUES
            (target_tenant_id, target_company_id, fiscal_year_start, fiscal_year_end, 'Open')
        RETURNING "Id" INTO fiscal_year_id;
    END IF;

    period_start := fiscal_year_start;
    WHILE period_start <= fiscal_year_end LOOP
        period_end := LEAST(
            (period_start + INTERVAL '1 month' - INTERVAL '1 day')::date,
            fiscal_year_end
        );

        SELECT COUNT(*)::integer
        INTO overlapping_period_count
        FROM "AccountingPeriods"
        WHERE "TenantId" = target_tenant_id
          AND "CompanyId" = target_company_id
          AND "FiscalYearId" = fiscal_year_id
          AND "StartsOn" <= period_end
          AND "EndsOn" >= period_start;

        IF overlapping_period_count > 1 THEN
            RAISE EXCEPTION 'Multiple accounting periods overlap % to %.',
                period_start, period_end;
        ELSIF overlapping_period_count = 1 THEN
            SELECT "StartsOn", "EndsOn", "Status"
            INTO existing_period_start, existing_period_end, existing_period_status
            FROM "AccountingPeriods"
            WHERE "TenantId" = target_tenant_id
              AND "CompanyId" = target_company_id
              AND "FiscalYearId" = fiscal_year_id
              AND "StartsOn" <= period_end
              AND "EndsOn" >= period_start;

            IF existing_period_start <> period_start OR existing_period_end <> period_end THEN
                RAISE EXCEPTION 'An accounting period overlaps % to % but has different dates; resolve it in the application.',
                    period_start, period_end;
            END IF;

            IF existing_period_status <> 'Open' THEN
                RAISE EXCEPTION 'Accounting period % to % is closed; reopen it in the application before seeding opening balances.',
                    period_start, period_end;
            END IF;
        ELSE
            INSERT INTO "AccountingPeriods"
                ("TenantId", "CompanyId", "FiscalYearId", "StartsOn", "EndsOn", "Status")
            VALUES
                (target_tenant_id, target_company_id, fiscal_year_id, period_start, period_end, 'Open');
        END IF;

        period_start := period_end + 1;
    END LOOP;

    RAISE NOTICE 'Open July-June fiscal year % to % (ID %) and monthly periods are ready.',
        fiscal_year_start, fiscal_year_end, fiscal_year_id;
END;
$create_fiscal_calendar$;
