import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { TaxRegistrationService } from './tax-registration.service';

describe('TaxRegistrationService', () => {
  const registration = {
    id: 5,
    companyId: 2,
    jurisdictionId: 3,
    code: 'VAT',
    countryCode: 'GB',
    registrationNumber: 'GB123',
  };
  const mutation = {
    id: 5,
    companyId: 2,
    jurisdictionId: 3,
    registrationNumber: 'GB123',
  };

  it('uses company-scoped endpoints for CRUD', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(endpoint.endsWith('/5') ? registration : [registration]);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of(mutation);
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of(registration);
      },
    } as unknown as ApiService;
    const service = new TaxRegistrationService(api);
    const payload = { jurisdictionId: 3, registrationNumber: 'GB123' };

    service.getAll().subscribe();
    service.getById(5).subscribe();
    service.create(payload).subscribe();
    service.update(5, payload).subscribe();

    expect(calls).toEqual([
      ['GET', 'tax-registrations', true],
      ['GET', 'tax-registrations/5', true],
      ['POST', 'tax-registrations', payload, true],
      ['PUT', 'tax-registrations/5', payload, true],
    ]);
  });

  it('rejects malformed registration records', () => {
    const api = ({
      get: () => of([{ ...registration, jurisdictionId: 'invalid' }]),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new TaxRegistrationService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
