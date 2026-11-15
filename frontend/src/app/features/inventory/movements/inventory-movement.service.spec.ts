import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { InventoryMovementService } from './inventory-movement.service';

describe('InventoryMovementService', () => {
  it('loads company-scoped movements and unwraps paginated results', () => {
    const calls: unknown[][] = [];
    const row = {
      id: 1,
      movementNumber: 'RCV-1',
      movementType: 'Receipt',
      movementDate: '2026-11-15',
      itemId: 2,
      warehouseId: 3,
      quantityIn: '4.00',
      quantityOut: '0.00',
      totalCost: '24.00',
    };
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push([endpoint, companyScoped]);
        return of({ items: [row], totalCount: 1 });
      },
    } as unknown as ApiService;
    let rows: unknown;

    new InventoryMovementService(api).getAll().subscribe(result => rows = result);

    expect(calls).toEqual([['inventory/movements', true]]);
    expect(rows).toEqual([row]);
  });

  it('rejects malformed movement records', () => {
    const api = ({ get: () => of([{ id: 1, movementNumber: 'RCV-1' }]) }) as unknown as ApiService;
    let receivedError: unknown;

    new InventoryMovementService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
