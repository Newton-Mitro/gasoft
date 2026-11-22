import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { WarehouseService } from './warehouse.service';

describe('WarehouseService', () => {
  it('uses company-scoped warehouse endpoints for CRUD operations', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        if (endpoint.endsWith('/locations/8')) {
          return of({ id: 8, warehouseId: 3, code: 'A1', name: 'Aisle 1' });
        }
        if (endpoint.endsWith('/locations')) {
          return of([{ id: 8, warehouseId: 3, code: 'A1', name: 'Aisle 1' }]);
        }
        return of(endpoint.endsWith('/3') ? { id: 3, code: 'MAIN', name: 'Main warehouse' } : [
          { id: 3, code: 'MAIN', name: 'Main warehouse' },
        ]);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of({ id: 3, code: 'MAIN', name: 'Main warehouse' });
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        if (endpoint.endsWith('/locations/8')) {
          return of({ id: 8, warehouseId: 3, ...(body as { code: string; name: string }) });
        }
        return of({ id: 3, code: 'MAIN', name: 'Main warehouse' });
      },
    } as unknown as ApiService;
    const service = new WarehouseService(api);
    const payload = { code: 'MAIN', name: 'Main warehouse' };

    service.getAll().subscribe();
    service.getById(3).subscribe();
    service.getLocations(3).subscribe();
    service.getLocation(3, 8).subscribe();
    service.create(payload).subscribe();
    service.update(3, payload).subscribe();
    service.updateLocation(3, 8, { code: 'A1', name: 'Aisle 1' }).subscribe();

    expect(calls).toEqual([
      ['GET', 'warehouses', true],
      ['GET', 'warehouses/3', true],
      ['GET', 'warehouses/3/locations', true],
      ['GET', 'warehouses/3/locations/8', true],
      ['POST', 'warehouses', payload, true],
      ['PUT', 'warehouses/3', payload, true],
      ['PUT', 'warehouses/3/locations/8', { code: 'A1', name: 'Aisle 1' }, true],
    ]);
  });

  it('rejects malformed warehouse list responses', () => {
    const api = ({ get: () => of([{ id: 3, code: 'MAIN' }]) }) as unknown as ApiService;
    let receivedError: unknown;

    new WarehouseService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });

  it('rejects malformed warehouse location responses', () => {
    const api = ({ get: () => of([{ id: 8, code: 'A1', name: 'Aisle 1' }]) }) as unknown as ApiService;
    let receivedError: unknown;

    new WarehouseService(api).getLocations(3).subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
