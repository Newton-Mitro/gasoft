import { of } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { BudgetService } from './budget.service';

describe('BudgetService', () => {
  const setup = {
    fiscalYears: [{ id: 2, startsOn: '2026-01-01', endsOn: '2026-12-31', status: 'Open' }],
    budgets: [{ id: 4, name: 'Base plan', fiscalYearId: 2 }],
    periods: [{ id: 5, startsOn: '2026-01-01', endsOn: '2026-01-31', status: 'Open' }],
    accounts: [{ accountId: 8, code: '4000', name: 'Sales', accountType: 'Revenue' }],
  };
  const report = {
    budget: setup.budgets[0],
    currencyCode: 'USD',
    periods: setup.periods,
    accounts: [{
      accountId: 8,
      accountCode: '4000',
      accountName: 'Sales',
      accountType: 'Revenue',
      periods: [{ periodId: 5, budget: 100, actual: 120, variance: 20, favorableVariance: 20 }],
      budget: 100,
      actual: 120,
      favorableVariance: 20,
    }],
    summary: {
      revenueBudget: 100,
      revenueActual: 120,
      expenseBudget: 0,
      expenseActual: 0,
      netIncomeBudget: 100,
      netIncomeActual: 120,
    },
  };

  it('uses company-scoped budget endpoints and passes fiscal-year and report filters', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean, params?: { toString(): string }) => {
        calls.push(['GET', endpoint, companyScoped, params?.toString()]);
        return of(endpoint === 'budgets' ? setup : report);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of(setup.budgets[0]);
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of(undefined);
      },
      delete: (endpoint: string, companyScoped: boolean) => {
        calls.push(['DELETE', endpoint, companyScoped]);
        return of(undefined);
      },
    } as unknown as ApiService;
    const service = new BudgetService(api);

    service.getSetup(2).subscribe();
    service.create('Base plan', 2).subscribe();
    service.saveLines(4, [{ accountId: 8, accountingPeriodId: 5, amount: 100 }]).subscribe();
    service.getBudgetVsActual(4).subscribe();
    service.delete(4).subscribe();

    expect(calls).toEqual([
      ['GET', 'budgets', true, 'fiscalYearId=2'],
      ['POST', 'budgets', { name: 'Base plan', fiscalYearId: 2 }, true],
      ['PUT', 'budgets/4/lines', { lines: [{ accountId: 8, accountingPeriodId: 5, amount: 100 }] }, true],
      ['GET', 'reports/budget-vs-actual', true, 'budgetId=4'],
      ['DELETE', 'budgets/4', true],
    ]);
  });

  it('rejects malformed budget report data', () => {
    const api = ({
      get: () => of({ ...report, summary: { ...report.summary, netIncomeActual: 'invalid' } }),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new BudgetService(api).getBudgetVsActual(4).subscribe({
      error: error => receivedError = error,
    });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid report data');
  });
});
