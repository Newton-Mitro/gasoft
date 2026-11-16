import { of } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { FinancialReportService } from './report.service';

describe('FinancialReportService', () => {
  const trialBalance = {
    companyId: 3,
    currencyCode: 'USD',
    from: null,
    to: null,
    accounts: [{
      accountId: 12,
      accountCode: '1000',
      accountName: 'Cash',
      accountType: 'Asset',
      debit: 100,
      credit: 0,
    }],
    totalDebit: 100,
    totalCredit: 0,
  };

  it('uses company-scoped endpoints and passes report date filters', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean, params: { toString(): string }) => {
        calls.push(['GET', endpoint, companyScoped, params.toString()]);
        return of(endpoint.endsWith('trial-balance')
          ? trialBalance
          : endpoint.endsWith('income-statement')
            ? {
                companyId: 3,
                currencyCode: 'USD',
                from: '2026-01-01',
                to: '2026-12-31',
                accounts: [],
                totalRevenue: 200,
                totalExpenses: 75,
                netIncome: 125,
              }
            : {
                companyId: 3,
                currencyCode: 'USD',
                asOfDate: '2026-12-31',
                accounts: [],
                totalAssets: 300,
                totalLiabilities: 100,
                totalEquity: 150,
                currentEarnings: 50,
                totalLiabilitiesAndEquity: 300,
                balanceDifference: 0,
              });
      },
    } as unknown as ApiService;
    const service = new FinancialReportService(api);

    service.getTrialBalance({ fromDate: '2026-01-01', toDate: '2026-12-31' }).subscribe();
    service.getIncomeStatement({ fromDate: '2026-01-01', toDate: '2026-12-31' }).subscribe();
    service.getBalanceSheet({ asOfDate: '2026-12-31' }).subscribe();

    expect(calls).toEqual([
      ['GET', 'reports/trial-balance', true, 'fromDate=2026-01-01&toDate=2026-12-31'],
      ['GET', 'reports/income-statement', true, 'fromDate=2026-01-01&toDate=2026-12-31'],
      ['GET', 'reports/balance-sheet', true, 'asOfDate=2026-12-31'],
    ]);
  });

  it('rejects malformed report account data', () => {
    const api = ({
      get: () => of({ ...trialBalance, accounts: [{ ...trialBalance.accounts[0], debit: 'invalid' }] }),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new FinancialReportService(api).getTrialBalance({}).subscribe({
      error: error => receivedError = error,
    });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid report data');
  });

  it('loads receivables and payables aging with an as-of date', () => {
    const calls: unknown[][] = [];
    const agingReport = {
      companyId: 3,
      companyFunctionalCurrencyCode: 'USD',
      asOfDate: '2026-11-16',
      documents: [{
        documentId: 8,
        documentNumber: 'INV-8',
        partyNumber: 'C-8',
        partyName: 'Customer',
        documentDate: '2026-10-01',
        dueDate: '2026-10-31',
        currencyCode: 'EUR',
        originalAmount: 125,
        outstandingAmount: 75,
        daysPastDue: 16,
        agingBucket: '1-30 days',
      }],
      totalsByCurrency: [{
        currencyCode: 'EUR',
        current: 0,
        days1To30: 75,
        days31To60: 0,
        days61To90: 0,
        over90Days: 0,
        total: 75,
      }],
    };
    const api = {
      get: (endpoint: string, companyScoped: boolean, params: { toString(): string }) => {
        calls.push([endpoint, companyScoped, params.toString()]);
        return of(agingReport);
      },
    } as unknown as ApiService;
    const service = new FinancialReportService(api);

    service.getReceivablesAging('2026-11-16').subscribe();
    service.getPayablesAging('2026-11-16').subscribe();

    expect(calls).toEqual([
      ['reports/receivables-aging', true, 'asOfDate=2026-11-16'],
      ['reports/payables-aging', true, 'asOfDate=2026-11-16'],
    ]);
  });

  it('loads cash flow with company-scoped date filters', () => {
    const calls: unknown[][] = [];
    const cashFlowReport = {
      companyId: 3,
      currencyCode: 'USD',
      from: '2026-01-01',
      to: '2026-11-16',
      cashAccounts: [{
        accountId: 12,
        accountCode: '1000',
        accountName: 'Operating cash',
        bankName: 'Example Bank',
        openingBalance: 100,
        cashChange: 25,
        closingBalance: 125,
      }],
      direct: {
        operating: { category: 'Operating', inflows: 50, outflows: 25, net: 25, accounts: [] },
        investing: { category: 'Investing', inflows: 0, outflows: 0, net: 0, accounts: [] },
        financing: { category: 'Financing', inflows: 0, outflows: 0, net: 0, accounts: [] },
        unclassified: { category: 'Unclassified', inflows: 0, outflows: 0, net: 0, accounts: [] },
        netCashIncrease: 25,
      },
      indirect: {
        netIncome: 15,
        nonCashAdjustments: 10,
        workingCapitalAdjustments: 0,
        operatingCashFlow: 25,
        investingCashFlow: 0,
        financingCashFlow: 0,
        unclassifiedCashFlow: 0,
        unclassifiedInflows: 0,
        unclassifiedOutflows: 0,
        netCashIncrease: 25,
        actualCashMovement: 25,
        reconciliationDifference: 0,
      },
      openingBalance: 100,
      closingBalance: 125,
      actualCashMovement: 25,
    };
    const api = {
      get: (endpoint: string, companyScoped: boolean, params: { toString(): string }) => {
        calls.push([endpoint, companyScoped, params.toString()]);
        return of(cashFlowReport);
      },
    } as unknown as ApiService;

    new FinancialReportService(api).getCashFlow({
      fromDate: '2026-01-01',
      toDate: '2026-11-16',
    }).subscribe();

    expect(calls).toEqual([
      ['reports/cash-flow', true, 'fromDate=2026-01-01&toDate=2026-11-16'],
    ]);
  });

  it('loads and updates cash flow account classifications', () => {
    const calls: unknown[][] = [];
    const account = {
      accountId: 12,
      code: '1000',
      name: 'Cash',
      accountType: 'Asset',
      cashFlowCategory: null,
      isCashAccount: false,
    };
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of([account]);
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of({ accountId: 12, category: 'Operating' });
      },
    } as unknown as ApiService;
    const service = new FinancialReportService(api);

    service.getCashFlowClassifications().subscribe();
    service.updateCashFlowClassification(12, 'Operating').subscribe();

    expect(calls).toEqual([
      ['GET', 'reports/cash-flow/accounts', true],
      ['PUT', 'reports/cash-flow/accounts/12', { category: 'Operating' }, true],
    ]);
  });
});
