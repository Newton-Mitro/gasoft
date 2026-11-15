import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { ItemCategoryService } from './item-category.service';

describe('ItemCategoryService', () => {
  it('uses the tenant-wide category endpoint for CRUD requests', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(endpoint.endsWith('/7') ? { id: 7, code: 'RAW', name: 'Raw materials' } : [
          { id: 7, code: 'RAW', name: 'Raw materials' },
        ]);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of({ id: 7, code: 'RAW', name: 'Raw materials' });
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of({ id: 7, code: 'RAW', name: 'Raw materials' });
      },
    } as unknown as ApiService;
    const service = new ItemCategoryService(api);
    const payload = { code: 'RAW', name: 'Raw materials' };

    service.getAll().subscribe();
    service.getById(7).subscribe();
    service.create(payload).subscribe();
    service.update(7, payload).subscribe();

    expect(calls).toEqual([
      ['GET', '/api/v1/item-categories', false],
      ['GET', '/api/v1/item-categories/7', false],
      ['POST', '/api/v1/item-categories', payload, false],
      ['PUT', '/api/v1/item-categories/7', payload, false],
    ]);
  });

  it('rejects malformed category list responses', () => {
    const api = ({ get: () => of([{ id: 1, code: 'RAW' }]) }) as unknown as ApiService;
    let receivedError: unknown;

    new ItemCategoryService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
