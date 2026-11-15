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
});
