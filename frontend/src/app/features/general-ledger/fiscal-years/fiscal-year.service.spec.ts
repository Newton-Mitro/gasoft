import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { FiscalYear, isIsoDateOnly } from './fiscal-year.model';
import { FiscalYearService } from './fiscal-year.service';

describe('FiscalYearService', () => {
  it('accepts only real ISO calendar dates for fiscal-year request values', () => {
    expect(isIsoDateOnly('2026-11-17')).toBe(true);
    expect(isIsoDateOnly('2026-02-29')).toBe(false);
    expect(isIsoDateOnly('17/11/2026')).toBe(false);
    expect(isIsoDateOnly('')).toBe(false);
  });

  it('loads fiscal years from the company API', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['get', endpoint, companyScoped]);
        return of([{ id: 1, startsOn: '2026-01-01', endsOn: '2026-12-31', status: 'Open' }]);
      },
    } as unknown as ApiService;
    const service = new FiscalYearService(api);
    let rows: FiscalYear[] = [];

    service.getAll().subscribe(result => rows = result);

    expect(calls).toEqual([['get', 'fiscal-years', true]]);
    expect(rows).toEqual([{ id: 1, startsOn: '2026-01-01', endsOn: '2026-12-31', status: 'Open' }]);
  });

  it('rejects malformed fiscal year responses rather than hiding records', () => {
    const api = { get: () => of([{ id: 1 }]) } as unknown as ApiService;
    let receivedError: unknown;

    new FiscalYearService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });

  it('uses the company-scoped fiscal-year close, reopen, checklist, and history endpoints', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(endpoint.endsWith('status-history') ? [] : {
          fiscalYearId: 3,
          accountingPeriodCount: 12,
          openAccountingPeriodCount: 0,
          draftJournalEntryCount: 0,
          closingJournalRequired: false,
          closingJournalEntryId: null,
          closingJournalStatus: null,
          closingJournalLines: [],
          canClose: true,
        });
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of({ id: 3, startsOn: '2026-01-01', endsOn: '2026-12-31', status: 'Closed' });
      },
    } as unknown as ApiService;
    const service = new FiscalYearService(api);

    service.getCloseChecklist(3).subscribe();
    service.prepareClosingJournal(3, 10).subscribe();
    service.prepareOpeningBalances(4).subscribe();
    service.prepareInitialOpeningBalances(5, [{ accountId: 11, debit: 100, credit: 0 }]).subscribe();
    service.postClosingJournal(17).subscribe();
    service.close(3, 'Year end complete').subscribe();
    service.reopen(3, 'Audit adjustment').subscribe();
    service.getStatusHistory(3).subscribe();

    expect(calls).toEqual([
      ['GET', 'fiscal-years/3/close-checklist', true],
      ['POST', 'fiscal-years/3/closing-journal', { retainedEarningsAccountId: 10 }, true],
      ['POST', 'fiscal-years/4/opening-balances', {}, true],
      ['POST', 'fiscal-years/5/initial-opening-balances', {
        lines: [{ accountId: 11, debit: 100, credit: 0 }],
      }, true],
      ['POST', 'journal-entries/17/post', {}, true],
      ['POST', 'fiscal-years/3/close', { reason: 'Year end complete' }, true],
      ['POST', 'fiscal-years/3/reopen', { reason: 'Audit adjustment' }, true],
      ['GET', 'fiscal-years/3/status-history', true],
    ]);
  });
});
