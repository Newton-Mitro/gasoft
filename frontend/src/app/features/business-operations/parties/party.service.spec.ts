import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PartyService } from './party.service';

describe('PartyService', () => {
  it('uses the tenant-wide party endpoint for CRUD requests', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        const row = {
          id: 5,
          partyNo: 'C-5',
          displayName: 'Example customer',
          isCustomer: true,
          isSupplier: false,
          email: null,
          phone: null,
          defaultCurrencyCode: 'USD',
        };
        return of(endpoint.endsWith('/5') ? row : [row]);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of({ id: 5 });
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of({ id: 5 });
      },
    } as unknown as ApiService;
    const service = new PartyService(api);
    const payload = {
      partyNo: 'C-5',
      displayName: 'Example customer',
      isCustomer: true,
      isSupplier: false,
      email: null,
      phone: null,
      defaultCurrencyCode: 'USD',
    };

    service.getAll().subscribe();
    service.getById(5).subscribe();
    service.create(payload).subscribe();
    service.update(5, payload).subscribe();

    expect(calls).toEqual([
      ['GET', '/api/v1/parties', false],
      ['GET', '/api/v1/parties/5', false],
      ['POST', '/api/v1/parties', payload, false],
      ['PUT', '/api/v1/parties/5', payload, false],
    ]);
  });

  it('gets or creates the tenant-wide walk-in customer', () => {
    const calls: unknown[][] = [];
    const walkIn = {
      id: 8,
      partyNo: 'WALK-IN',
      displayName: 'Walk-in Customer',
      isCustomer: true,
      isSupplier: false,
      email: null,
      phone: null,
      defaultCurrencyCode: null,
    };
    const api = {
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push([endpoint, body, companyScoped]);
        return of(walkIn);
      },
    } as unknown as ApiService;
    let result: unknown;

    new PartyService(api).getWalkInCustomer().subscribe(value => result = value);

    expect(result).toEqual(walkIn);
    expect(calls).toEqual([['/api/v1/parties/walk-in', {}, false]]);
  });

  it('rejects malformed party records', () => {
    const api = ({ get: () => of([{ id: 5, partyNo: 'C-5' }]) }) as unknown as ApiService;
    let receivedError: unknown;

    new PartyService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
