import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { WarehouseService } from './warehouse.service';

describe('WarehouseService', () => {
  it('uses company-scoped warehouse endpoints for CRUD operations', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
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
        return of({ id: 3, code: 'MAIN', name: 'Main warehouse' });
      },
    } as unknown as ApiService;
    const service = new WarehouseService(api);
    const payload = { code: 'MAIN', name: 'Main warehouse' };

    service.getAll().subscribe();
    service.getById(3).subscribe();
    service.create(payload).subscribe();
    service.update(3, payload).subscribe();

    expect(calls).toEqual([
      ['GET', 'warehouses', true],
      ['GET', 'warehouses/3', true],
      ['POST', 'warehouses', payload, true],
      ['PUT', 'warehouses/3', payload, true],
    ]);
  });

  it('rejects malformed warehouse list responses', () => {
    const api = ({ get: () => of([{ id: 3, code: 'MAIN' }]) }) as unknown as ApiService;
    let receivedError: unknown;

    new WarehouseService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
