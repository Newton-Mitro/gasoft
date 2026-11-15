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
});
