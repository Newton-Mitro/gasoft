import { of } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { OptionDataService } from './option-data.service';
import { OptionDataCacheService } from './option-data-cache.service';

describe('OptionDataService', () => {
  function createService(response: unknown): OptionDataService {
    const api = {
      get: () => of(response),
    } as unknown as ApiService;
    return new OptionDataService(api, new OptionDataCacheService());
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

  it('limits bank reconciliation account options to active posting asset accounts', () => {
    const api = {
      get: (endpoint: string) => of(endpoint === 'accounts'
        ? [
            { id: 3, accountGroupId: 10, code: '1000', name: 'Cash', allowPosting: true, isActive: true },
            { id: 4, accountGroupId: 20, code: '2000', name: 'Payables', allowPosting: true, isActive: true },
            { id: 5, accountGroupId: 10, code: '1010', name: 'Cash control', allowPosting: false, isActive: true },
          ]
        : [
            { id: 10, accountType: 'Asset' },
            { id: 20, accountType: 'Liability' },
          ]),
    } as unknown as ApiService;
    const service = new OptionDataService(api, new OptionDataCacheService());
    let options: { label: string; value: string | number }[] = [];

    service.getBankAssetAccountOptions().subscribe(result => options = result);

    expect(options).toEqual([{ label: '1000 · Cash', value: 3 }]);
  });

  it('reuses loaded option data until the cache is cleared', () => {
    let requestCount = 0;
    const api = {
      get: () => {
        requestCount += 1;
        return of([{ code: 'USD', name: 'US Dollar' }]);
      },
    } as unknown as ApiService;
    const service = new OptionDataService(api, new OptionDataCacheService());

    service.getOptions('currencies').subscribe();
    service.getOptions('currencies').subscribe();

    expect(requestCount).toBe(1);
  });

  it('loads jurisdictions globally and company tax codes/categories from the tax reference', () => {
    let requestCount = 0;
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        requestCount += 1;
        if (endpoint === '/api/v1/tax-jurisdictions') {
          expect(companyScoped).toBe(false);
          return of([{ id: 4, code: 'US', countryCode: 'US' }]);
        }
        expect(endpoint).toBe('tax-reference');
        expect(companyScoped).toBe(true);
        return of({
          categories: [{ id: 2, code: 'STANDARD', name: 'Standard' }],
          codes: [{ id: 8, code: 'VAT', taxType: 'Sales' }],
        });
      },
    } as unknown as ApiService;
    const service = new OptionDataService(api, new OptionDataCacheService());
    let jurisdictions: { label: string; value: string | number }[] = [];
    let taxCodes: { label: string; value: string | number }[] = [];
    let taxCategories: { label: string; value: string | number }[] = [];

    service.getOptions('jurisdictions').subscribe(result => jurisdictions = result);
    service.getOptions('taxCodes').subscribe(result => taxCodes = result);
    service.getOptions('taxCategories').subscribe(result => taxCategories = result);

    expect(requestCount).toBe(2);
    expect(jurisdictions).toEqual([{ label: 'US · US', value: 4 }]);
    expect(taxCodes).toEqual([{ label: 'VAT · Sales', value: 8 }]);
    expect(taxCategories).toEqual([{ label: 'STANDARD · Standard', value: 2 }]);
  });

});
