import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { AccountingPeriod } from './period.model';
import { AccountingPeriodService } from './period.service';

describe('AccountingPeriodService', () => {
  it('loads periods from the company API and rejects malformed rows', () => {
    const api = {
      get: () => of({ items: [{
        id: 2,
        fiscalYearId: 1,
        startsOn: '2026-01-01',
        endsOn: '2026-01-31',
        status: 'Open',
      }] }),
    } as unknown as ApiService;
    let rows: AccountingPeriod[] = [];

    new AccountingPeriodService(api).getAll().subscribe(result => rows = result);

    expect(rows).toEqual([{
      id: 2,
      fiscalYearId: 1,
      startsOn: '2026-01-01',
      endsOn: '2026-01-31',
      status: 'Open',
    }]);

    const malformedApi = { get: () => of([{ id: 2 }]) } as unknown as ApiService;
    let receivedError: unknown;
    new AccountingPeriodService(malformedApi).getAll()
      .subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });

  it('uses the company-scoped close, reopen, and audit-history endpoints', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of([]);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of({
          id: 2,
          fiscalYearId: 1,
          startsOn: '2026-01-01',
          endsOn: '2026-01-31',
          status: endpoint.endsWith('/close') ? 'Closed' : 'Open',
        });
      },
    } as unknown as ApiService;
    const service = new AccountingPeriodService(api);

    service.close(2, 'Month-end complete').subscribe();
    service.reopen(2, 'Correction required').subscribe();
    service.getStatusHistory(2).subscribe();

    expect(calls).toEqual([
      ['POST', 'accounting-periods/2/close', { reason: 'Month-end complete' }, true],
      ['POST', 'accounting-periods/2/reopen', { reason: 'Correction required' }, true],
      ['GET', 'accounting-periods/2/status-history', true],
    ]);
  });

  it('loads the close checklist and validates its response shape', () => {
    const api = {
      get: () => of({
        periodId: 2,
        draftJournalEntryCount: 1,
        unreconciledBankTransactionCount: 3,
        canClose: false,
      }),
    } as unknown as ApiService;
    let checklist: unknown;

    new AccountingPeriodService(api).getCloseChecklist(2).subscribe(value => checklist = value);
    expect(checklist).toEqual({
      periodId: 2,
      draftJournalEntryCount: 1,
      unreconciledBankTransactionCount: 3,
      canClose: false,
    });

    const malformedApi = { get: () => of({ periodId: 2, canClose: true }) } as unknown as ApiService;
    let receivedError: unknown;
    new AccountingPeriodService(malformedApi).getCloseChecklist(2)
      .subscribe({ error: error => receivedError = error });
    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid data');
  });
});
