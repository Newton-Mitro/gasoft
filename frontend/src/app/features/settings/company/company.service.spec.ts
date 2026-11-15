import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { CompanyService } from './company.service';

describe('CompanyService', () => {
  const company = {
    id: 3,
    code: 'ACME',
    legalName: 'Acme Limited',
    functionalCurrencyCode: 'USD',
  };

  it('uses the selected company endpoint without company URL scoping', () => {
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
    } as unknown as ApiService;
    const service = new CompanyService(api);
    const payload = { legalName: 'Acme Limited', functionalCurrencyCode: 'USD' };

    service.getById(3).subscribe();
    service.update(3, payload).subscribe();

    expect(calls).toEqual([
      ['GET', '/api/v1/companies/3', false],
      ['PUT', '/api/v1/companies/3', payload, false],
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
