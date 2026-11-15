import { of } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { OptionDataService } from './option-data.service';

describe('OptionDataService', () => {
  function createService(response: unknown): OptionDataService {
    const api = {
      get: () => of(response),
    } as unknown as ApiService;
    return new OptionDataService(api);
  }

  it('formats accounting periods as readable date ranges', () => {
    const service = createService([{
      id: 8,
      fiscalYearId: 2,
      startsOn: '2026-01-01',
      endsOn: '2026-01-31',
      status: 'Open',
    }]);
    let options: { label: string; value: string | number }[] = [];

    service.getOptions('periods').subscribe(result => options = result);

    expect(options).toHaveLength(1);
    expect(options[0].label).toContain('2026');
    expect(options[0].label).toContain('Open');
    expect(options[0].label).not.toContain('undefined');
    expect(options[0].value).toBe(8);
  });

  it('offers only active posting accounts with valid display fields', () => {
    const service = createService([
      { id: 3, code: '1000', name: 'Cash', allowPosting: true, isActive: true },
      { id: 4, code: '1100', name: 'Receivables', allowPosting: false, isActive: true },
      { id: 5, code: '1200', name: 'Inventory', allowPosting: true, isActive: false },
      { id: 0, code: '1300', name: 'Tax', allowPosting: true, isActive: true },
      { id: 7, allowPosting: true, isActive: true },
    ]);
    let options: { label: string; value: string | number }[] = [];

    service.getOptions('accounts').subscribe(result => options = result);

    expect(options).toEqual([{ label: '1000 · Cash', value: 3 }]);
  });

});
