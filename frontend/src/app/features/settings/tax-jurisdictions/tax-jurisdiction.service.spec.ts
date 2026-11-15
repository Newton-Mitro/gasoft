import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { TaxJurisdictionService } from './tax-jurisdiction.service';

describe('TaxJurisdictionService', () => {
  const jurisdiction = { id: 2, code: 'US-CA', countryCode: 'US' };

  it('uses the owner-managed tax jurisdiction endpoints', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(endpoint.endsWith('/2') ? jurisdiction : [jurisdiction]);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of(jurisdiction);
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of(jurisdiction);
      },
      delete: (endpoint: string, companyScoped: boolean) => {
        calls.push(['DELETE', endpoint, companyScoped]);
        return of(undefined);
      },
    } as unknown as ApiService;
    const service = new TaxJurisdictionService(api);
    const payload = { code: 'US-CA', countryCode: 'US' };

    service.getAll().subscribe();
    service.getById(2).subscribe();
    service.create(payload).subscribe();
    service.update(2, payload).subscribe();
    service.delete(2).subscribe();

    expect(calls).toEqual([
      ['GET', '/api/v1/tax-jurisdictions', false],
      ['GET', '/api/v1/tax-jurisdictions/2', false],
      ['POST', '/api/v1/tax-jurisdictions', payload, false],
      ['PUT', '/api/v1/tax-jurisdictions/2', payload, false],
      ['DELETE', '/api/v1/tax-jurisdictions/2', false],
    ]);
  });

  it('rejects malformed jurisdiction responses', () => {
    const api = {
      get: () => of({ id: 2, code: 'US-CA', countryCode: 23 }),
    } as unknown as ApiService;
    let receivedError: unknown;

    new TaxJurisdictionService(api).getById(2).subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
