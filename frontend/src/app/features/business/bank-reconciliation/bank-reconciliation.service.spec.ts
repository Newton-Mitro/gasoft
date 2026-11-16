import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { BankReconciliationService } from './bank-reconciliation.service';

describe('BankReconciliationService', () => {
  it('validates and returns partial reconciliation allocations', () => {
    const transactions = [{
      id: 8,
      transactionDate: '2026-11-15',
      description: 'Customer deposit',
      reference: null,
      amount: 125,
      matchedAmount: 50,
      remainingAmount: 75,
      matches: [{
        id: 12,
        journalLineId: 31,
        amount: 50,
        entryDate: '2026-11-15',
        description: 'Invoice receipt',
      }],
    }];
    const api = {
      get: () => of(transactions),
    } as unknown as ApiService;
    let response: unknown;

    new BankReconciliationService(api).getTransactions(4).subscribe(value => response = value);

    expect(response).toEqual(transactions);
  });

  it('sends partial allocations and adjustment details to bank reconciliation endpoints', () => {
    const calls: unknown[][] = [];
    const api = {
      post: (endpoint: string, body: unknown) => {
        calls.push([endpoint, body]);
        return of({});
      },
      delete: (endpoint: string) => {
        calls.push([endpoint]);
        return of(undefined);
      },
    } as unknown as ApiService;
    const service = new BankReconciliationService(api);

    service.allocate(4, 8, 31, 25).subscribe();
    service.createAdjustment(4, 8, {
      offsetAccountId: 42,
      entryDate: '2026-11-16',
      description: 'Bank service fee',
    }).subscribe();
    service.unmatch(4, 8, 12).subscribe();

    expect(calls).toEqual([
      ['bank-accounts/4/transactions/8/match', { journalLineId: 31, amount: 25 }],
      ['bank-accounts/4/transactions/8/adjustment', {
        offsetAccountId: 42,
        entryDate: '2026-11-16',
        description: 'Bank service fee',
      }],
      ['bank-accounts/4/transactions/8/matches/12'],
    ]);
  });
});
