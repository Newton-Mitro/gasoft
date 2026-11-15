import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { FiscalYear } from './fiscal-year.model';
import { FiscalYearService } from './fiscal-year.service';

describe('FiscalYearService', () => {
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
});
