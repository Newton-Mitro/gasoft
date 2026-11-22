import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { Journal } from './journal.model';
import { JournalService } from './journal.service';

describe('JournalService', () => {
  it('loads journals from the company API', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['get', endpoint, companyScoped]);
        return of([{ id: 2, code: 'GEN', name: 'General', journalType: 'General', isActive: true }]);
      },
    } as unknown as ApiService;
    let rows: Journal[] = [];

    new JournalService(api).getAll().subscribe(result => rows = result);

    expect(calls).toEqual([['get', 'journals', true]]);
    expect(rows).toEqual([{ id: 2, code: 'GEN', name: 'General', journalType: 'General', isActive: true }]);
  });

  it('reports malformed records instead of returning an incomplete list', () => {
    const api = { get: () => of([{ id: 2, code: 'GEN' }]) } as unknown as ApiService;
    let receivedError: unknown;

    new JournalService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });

  it('accepts cash journals returned by the API', () => {
    const journal: Journal = {
      id: 3,
      code: 'CASHREC',
      name: 'Cash receipts journal',
      journalType: 'Cash',
      isActive: true,
    };
    const api = { get: () => of([journal]) } as unknown as ApiService;
    let rows: Journal[] = [];

    new JournalService(api).getAll().subscribe(result => rows = result);

    expect(rows).toEqual([journal]);
  });
});
