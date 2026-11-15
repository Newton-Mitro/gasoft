import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { InventoryCountService } from './inventory-count.service';

describe('InventoryCountService', () => {
  it('uses company-scoped endpoints for inventory count CRUD', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        const row = {
          id: 8,
          countNumber: 'COUNT-8',
          countDate: '2026-11-15',
          warehouseId: 3,
          status: 'Draft',
          items: [{ itemId: 9, countedQuantity: '8.00', adjustmentUnitCost: null }],
        };
        return of(endpoint.endsWith('/8') ? row : [row]);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of({ id: 8 });
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of({ id: 8 });
      },
    } as unknown as ApiService;
    const service = new InventoryCountService(api);
    const payload = {
      warehouseId: 3,
      countNumber: 'COUNT-8',
      countDate: '2026-11-15',
      items: [{ itemId: 9, countedQuantity: 8, adjustmentUnitCost: null }],
    };

    service.getAll().subscribe();
    service.getById(8).subscribe();
    service.create(payload).subscribe();
    service.update(8, payload).subscribe();

    expect(calls).toEqual([
      ['GET', 'inventory/counts', true],
      ['GET', 'inventory/counts/8', true],
      ['POST', 'inventory/counts', payload, true],
      ['PUT', 'inventory/counts/8', payload, true],
    ]);
  });

  it('rejects a count response with malformed item data', () => {
    const api = ({
      get: () => of([{
        id: 8,
        countNumber: 'COUNT-8',
        countDate: '2026-11-15',
        warehouseId: 3,
        status: 'Draft',
        items: [{ itemId: 9, countedQuantity: 'invalid', adjustmentUnitCost: null }],
      }]),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new InventoryCountService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
