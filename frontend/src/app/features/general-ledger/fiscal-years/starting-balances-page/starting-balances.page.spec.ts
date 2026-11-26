import { ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { OptionDataService } from '../../../../shared/data/option-data.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { FiscalYear } from '../fiscal-year.model';
import { FiscalYearService } from '../fiscal-year.service';
import { StartingBalancesPageComponent } from './starting-balances.page';

describe('StartingBalancesPageComponent', () => {
  const firstYear: FiscalYear = {
    id: 1,
    startsOn: '2026-01-01',
    endsOn: '2026-12-31',
    status: 'Open',
  };

  function createPage(
    year: FiscalYear,
    years: FiscalYear[],
  ): {
    page: StartingBalancesPageComponent;
    fiscalYears: { prepareInitialOpeningBalances: ReturnType<typeof vi.fn> };
    optionData: { getOpeningBalanceAccountOptions: ReturnType<typeof vi.fn> };
    notifications: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
  } {
    const fiscalYears = {
      getById: vi.fn(() => of(year)),
      getAll: vi.fn(() => of(years)),
      prepareInitialOpeningBalances: vi.fn(() => of({ id: 42, status: 'Draft' })),
    };
    const optionData = {
      getOpeningBalanceAccountOptions: vi.fn(() => of([{ label: '1000 · Cash', value: 10 }])),
    };
    const notifications = { success: vi.fn(), error: vi.fn() };
    const route = {
      paramMap: of(convertToParamMap({ recordId: String(year.id) })),
    } as unknown as ActivatedRoute;
    const page = new StartingBalancesPageComponent(
      route,
      { navigateByUrl: vi.fn() } as unknown as Router,
      fiscalYears as unknown as FiscalYearService,
      optionData as unknown as OptionDataService,
      notifications as unknown as NotificationService,
      { markForCheck: vi.fn() } as unknown as ChangeDetectorRef,
    );
    return { page, fiscalYears, optionData, notifications };
  }

  it('creates a starting-balance draft for the first open fiscal year', () => {
    const { page, fiscalYears, notifications } = createPage(firstYear, [firstYear]);

    page.ngOnInit();
    expect(page.year).toEqual(firstYear);
    page.setLines([{ accountId: 10, debit: 100, credit: 0 }]);
    expect(page.canCreate).toBe(true);

    page.createDraft();

    expect(fiscalYears.prepareInitialOpeningBalances).toHaveBeenCalledWith(1, [
      { accountId: 10, debit: 100, credit: 0 },
    ]);
    expect(page.result).toEqual({ id: 42, status: 'Draft' });
    expect(notifications.success).toHaveBeenCalled();
  });

  it('rejects starting balances for a fiscal year with a predecessor', () => {
    const previousYear: FiscalYear = {
      id: 1,
      startsOn: '2025-01-01',
      endsOn: '2025-12-31',
      status: 'Closed',
    };
    const currentYear: FiscalYear = {
      ...firstYear,
      id: 2,
      startsOn: '2026-01-01',
      endsOn: '2026-12-31',
    };
    const { page, optionData } = createPage(currentYear, [previousYear, currentYear]);

    page.ngOnInit();

    expect(page.year).toBeNull();
    expect(page.error).toContain('first fiscal year');
    expect(optionData.getOpeningBalanceAccountOptions).not.toHaveBeenCalled();
  });
});
