import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { Item } from './item.model';
import { ItemService } from './item.service';

describe('ItemService', () => {
  it('uses the selected company inventory API for listing items and prices', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push([endpoint, companyScoped]);
        return of([{
          id: 1,
          categoryId: 2,
          sku: 'SKU-1',
          name: 'Widget',
          baseUnitCode: 'EA',
          costingMethod: 'FIFO',
          unitSellingPrice: 12.5,
          sellingPriceCurrencyCode: 'USD',
        }]);
      },
    } as unknown as ApiService;
    let rows: Item[] = [];

    new ItemService(api).getAll().subscribe(result => rows = result);

    expect(calls).toEqual([['items', true]]);
    expect(rows[0]?.sku).toBe('SKU-1');
  });

  it('rejects malformed item responses', () => {
    const api = ({ get: () => of([{ id: 1 }]) }) as unknown as ApiService;
    let receivedError: unknown;

    new ItemService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
