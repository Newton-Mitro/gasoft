import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PartyAddressService } from './party-address.service';

describe('PartyAddressService', () => {
  it('uses the parent party address endpoint without company scoping', () => {
    const calls: unknown[][] = [];
    const row = {
      id: 2,
      addressType: 'Billing',
      line1: '10 Main Street',
      line2: null,
      city: 'Springfield',
      region: null,
      postalCode: null,
      countryCode: 'US',
      isDefault: true,
    };
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(endpoint.endsWith('/2') ? row : [row]);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of(row);
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of(row);
      },
    } as unknown as ApiService;
    const service = new PartyAddressService(api);
    const payload = {
      addressType: 'Billing' as const,
      line1: '10 Main Street',
      line2: null,
      city: 'Springfield',
      region: null,
      postalCode: null,
      countryCode: 'US',
      isDefault: true,
    };

    service.getAll(5).subscribe();
    service.getById(5, 2).subscribe();
    service.create(5, payload).subscribe();
    service.update(5, 2, payload).subscribe();

    expect(calls).toEqual([
      ['GET', '/api/v1/parties/5/addresses', false],
      ['GET', '/api/v1/parties/5/addresses/2', false],
      ['POST', '/api/v1/parties/5/addresses', payload, false],
      ['PUT', '/api/v1/parties/5/addresses/2', payload, false],
    ]);
  });

  it('rejects malformed address list responses', () => {
    const api = ({ get: () => of([{ id: 2, addressType: 'Billing' }]) }) as unknown as ApiService;
    let receivedError: unknown;

    new PartyAddressService(api).getAll(5).subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });

  it('accepts paginated address list responses', () => {
    const address = {
      id: 2,
      addressType: 'Billing',
      line1: '10 Main Street',
      line2: null,
      city: 'Springfield',
      region: null,
      postalCode: null,
      countryCode: 'US',
      isDefault: true,
    };
    const api = ({ get: () => of({ items: [address] }) }) as unknown as ApiService;
    let receivedAddresses: unknown;

    new PartyAddressService(api).getAll(5).subscribe(result => receivedAddresses = result);

    expect(receivedAddresses).toEqual([address]);
  });

  it('rejects malformed address detail responses', () => {
    const api = ({ get: () => of({ id: 2, addressType: 'Billing' }) }) as unknown as ApiService;
    let receivedError: unknown;

    new PartyAddressService(api).getById(5, 2).subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });

  it('rejects malformed address create and update responses', () => {
    const api = {
      post: () => of({ id: 2 }),
      put: () => of({ id: 2 }),
    } as unknown as ApiService;
    const service = new PartyAddressService(api);
    const payload = {
      addressType: 'Billing' as const,
      line1: '10 Main Street',
      line2: null,
      city: 'Springfield',
      region: null,
      postalCode: null,
      countryCode: 'US',
      isDefault: true,
    };
    const errors: unknown[] = [];

    service.create(5, payload).subscribe({ error: error => errors.push(error) });
    service.update(5, 2, payload).subscribe({ error: error => errors.push(error) });

    expect(errors).toHaveLength(2);
    expect(errors.every(error => error instanceof Error && error.message.includes('invalid record'))).toBe(true);
  });

  it('rejects invalid party and address IDs before making a request', () => {
    const get = vi.fn(() => of([]));
    const api = ({ get }) as unknown as ApiService;
    const service = new PartyAddressService(api);

    expect(() => service.getAll(0)).toThrow('A valid party ID is required.');
    expect(() => service.getById(5, 0)).toThrow('A valid address ID is required.');
    expect(() => service.create(0, {
      addressType: 'Billing',
      line1: '10 Main Street',
      line2: null,
      city: 'Springfield',
      region: null,
      postalCode: null,
      countryCode: 'US',
      isDefault: true,
    })).toThrow('A valid party ID is required.');
    expect(() => service.update(5, -1, {
      addressType: 'Billing',
      line1: '10 Main Street',
      line2: null,
      city: 'Springfield',
      region: null,
      postalCode: null,
      countryCode: 'US',
      isDefault: true,
    })).toThrow('A valid address ID is required.');
    expect(get).not.toHaveBeenCalled();
  });
});
