import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { CompanyService } from './company.service';

describe('CompanyService', () => {
  const company = {
    id: 3,
    code: 'ACME',
    legalName: 'Acme Limited',
    functionalCurrencyCode: 'USD',
    phone: null,
    email: null,
    website: null,
    address: null,
    cityRegion: null,
    postalCode: null,
    country: null,
    taxId: null,
    logoUrl: null,
  };

  it('uses the tenant company endpoint without company URL scoping', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(company);
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of(company);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body instanceof FormData ? true : body, companyScoped]);
        return of(company);
      },
    } as unknown as ApiService;
    const service = new CompanyService(api);
    const payload = {
      legalName: 'Acme Limited',
      functionalCurrencyCode: 'USD',
      phone: '',
      email: '',
      website: '',
      address: '',
      cityRegion: '',
      postalCode: '',
      country: '',
      taxId: '',
    };
    const createPayload = {
      code: 'NEWCO',
      legalName: 'New Company',
      functionalCurrencyCode: 'USD',
    };

    service.getById(3).subscribe();
    service.create(createPayload).subscribe();
    service.update(3, payload).subscribe();
    service.uploadLogo(3, new File(['image'], 'logo.png', { type: 'image/png' })).subscribe();

    expect(calls).toEqual([
      ['GET', '/api/v1/companies/3', false],
      ['POST', '/api/v1/tenant/companies', createPayload, false],
      ['PUT', '/api/v1/companies/3', payload, false],
      ['POST', '/api/v1/companies/3/logo', true, false],
    ]);
  });

  it('rejects malformed company records', () => {
    const api = ({
      get: () => of({ ...company, id: '3' }),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new CompanyService(api).getById(3).subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
