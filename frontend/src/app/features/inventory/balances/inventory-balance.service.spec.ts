import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { InventoryBalanceService } from './inventory-balance.service';

describe('InventoryBalanceService', () => {
  it('loads company-scoped balances and normalizes a paged response', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push([endpoint, companyScoped]);
        return of({
          items: [{ itemId: 12, warehouseId: 4, quantity: '5.00', value: '35.50' }],
          totalCount: 1,
        });
      },
    } as unknown as ApiService;
    let rows: unknown;

    new InventoryBalanceService(api).getAll().subscribe(result => rows = result);

    expect(calls).toEqual([['inventory/balances', true]]);
    expect(rows).toEqual([{ itemId: 12, warehouseId: 4, quantity: '5.00', value: '35.50' }]);
  });

  it('rejects malformed balance rows', () => {
    const api = ({ get: () => of([{ itemId: 12, warehouseId: 4, quantity: '5.00' }]) }) as unknown as ApiService;
    let receivedError: unknown;

    new InventoryBalanceService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
