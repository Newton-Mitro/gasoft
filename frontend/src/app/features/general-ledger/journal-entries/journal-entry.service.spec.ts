import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { JournalEntryService } from './journal-entry.service';

describe('JournalEntryService', () => {
  const entry = {
    id: 21,
    companyId: 3,
    journalId: 4,
    accountingPeriodId: 6,
    entryDate: '2026-11-15',
    description: 'Opening adjustment',
    status: 'Draft',
    totalDebit: '25.00',
    totalCredit: '25.00',
    reversalOfEntryId: null,
    reversedByEntryId: null,
    lines: [{
      id: 1,
      accountId: 10,
      transactionCurrencyCode: 'USD',
      debit: '25.00',
      credit: '0',
      functionalDebit: '25.00',
      functionalCredit: '0',
      exchangeRate: '1.000000',
      description: 'Opening adjustment',
    }],
  };

  it('uses company-scoped endpoints for CRUD', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(endpoint.endsWith('/21') ? entry : [entry]);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of(entry);
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of(entry);
      },
    } as unknown as ApiService;
    const service = new JournalEntryService(api);
    const payload = {
      journalId: 4,
      accountingPeriodId: 6,
      entryDate: '2026-11-15',
      description: 'Opening adjustment',
      lines: [{
        accountId: 10,
        transactionCurrencyCode: 'USD',
        debit: 25,
        credit: 0,
        exchangeRate: 1,
        description: 'Opening adjustment',
      }, {
        accountId: 11,
        transactionCurrencyCode: 'USD',
        debit: 0,
        credit: 25,
        exchangeRate: 1,
        description: 'Opening adjustment',
      }],
    };

    service.getAll().subscribe();
    service.getById(21).subscribe();
    service.create(payload).subscribe();
    service.update(21, payload).subscribe();
    service.reverse(21, '2026-11-23').subscribe();

    expect(calls).toEqual([
      ['GET', 'journal-entries', true],
      ['GET', 'journal-entries/21', true],
      ['POST', 'journal-entries', payload, true],
      ['PUT', 'journal-entries/21', payload, true],
      ['POST', 'journal-entries/21/reverse', { reversalDate: '2026-11-23' }, true],
    ]);
  });

  it('rejects malformed journal-entry line details', () => {
    const api = ({
      get: () => of([{ ...entry, lines: [{ ...entry.lines[0], functionalDebit: 'invalid' }] }]),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new JournalEntryService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });

  it('rejects malformed reversal references', () => {
    const api = ({
      get: () => of({ ...entry, reversedByEntryId: 'not-an-id' }),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new JournalEntryService(api).getById(21).subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
