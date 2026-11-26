import { ChangeDetectorRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { FinancialReportService } from '../../reports/data-access/report.service';
import {
  BalanceSheetReport,
  CashFlowReport,
  IncomeStatementReport,
} from '../../reports/data-access/report.model';
import { DashboardPage } from './dashboard.page';

describe('DashboardPage', () => {
  it('builds balance-sheet and cash-flow graphs from financial reports', () => {
    const income: IncomeStatementReport = {
      companyId: 1,
      currencyCode: 'USD',
      from: '2026-01-01',
      to: '2026-11-26',
      accounts: [],
      totalRevenue: 1200,
      totalExpenses: 700,
      netIncome: 500,
    };
    const balance: BalanceSheetReport = {
      companyId: 1,
      currencyCode: 'USD',
      asOfDate: '2026-11-26',
      accounts: [],
      totalAssets: 1000,
      totalLiabilities: 600,
      totalEquity: 400,
      currentEarnings: 500,
      totalLiabilitiesAndEquity: 1000,
      balanceDifference: 0,
    };
    const flow: CashFlowReport = {
      companyId: 1,
      currencyCode: 'USD',
      from: '2026-01-01',
      to: '2026-11-26',
      cashAccounts: [],
      direct: {
        operating: { category: 'Operating', inflows: 700, outflows: 200, net: 500, accounts: [] },
        investing: { category: 'Investing', inflows: 0, outflows: 200, net: -200, accounts: [] },
        financing: { category: 'Financing', inflows: 0, outflows: 0, net: 0, accounts: [] },
        unclassified: { category: 'Unclassified', inflows: 0, outflows: 0, net: 0, accounts: [] },
        netCashIncrease: 300,
      },
      indirect: {
        netIncome: 500,
        nonCashAdjustments: 0,
        workingCapitalAdjustments: 0,
        operatingCashFlow: 500,
        investingCashFlow: -200,
        financingCashFlow: 0,
        unclassifiedCashFlow: 0,
        unclassifiedInflows: 0,
        unclassifiedOutflows: 0,
        netCashIncrease: 300,
        actualCashMovement: 300,
        reconciliationDifference: 0,
      },
      openingBalance: 0,
      closingBalance: 300,
      actualCashMovement: 300,
    };
    const api = {
      get: (endpoint: string) => of(endpoint === 'reports/trial-balance'
        ? { currencyCode: 'USD' }
        : endpoint === 'accounts/page'
          ? { totalCount: 3, items: [] }
          : { totalCount: 5, items: [] }),
    };
    const reports = {
      getIncomeStatement: vi.fn(() => of(income)),
      getBalanceSheet: vi.fn(() => of(balance)),
      getCashFlow: vi.fn(() => of(flow)),
    };
    TestBed.configureTestingModule({
      providers: [{ provide: NotificationService, useValue: { error: vi.fn() } }],
    });
    const page = TestBed.runInInjectionContext(() => new DashboardPage(
      api as unknown as ApiService,
      reports as unknown as FinancialReportService,
      { markForCheck: vi.fn() } as unknown as ChangeDetectorRef,
    ));

    page.ngOnInit();

    expect(page.position().map(item => item.width)).toEqual([100, 60, 40]);
    expect(page.cashFlow().map(item => item.width)).toEqual([100, 40, 0, 0]);
    expect(page.cashFlow()[1].amount).toBe(-200);
    expect(page.summary()?.cashMovement).toBe(300);
  });
});
