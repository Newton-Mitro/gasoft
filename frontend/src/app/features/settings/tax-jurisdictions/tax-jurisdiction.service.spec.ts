import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { TaxJurisdictionService } from './tax-jurisdiction.service';

describe('TaxJurisdictionService', () => {
  const jurisdictions = [
    { id: 2, code: 'US', countryCode: 'US' },
    { id: 3, code: 'GB', countryCode: 'GB' },
  ];

  it('loads the read-only supported jurisdictions endpoint', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(jurisdictions);
      },
    } as unknown as ApiService;
    const service = new TaxJurisdictionService(api);

    service.getAll().subscribe();

    expect(calls).toEqual([
      ['GET', '/api/v1/tax-jurisdictions', false],
    ]);
  });

  it('rejects malformed jurisdiction responses', () => {
    const api = {
      get: () => of([{ id: 2, code: 'US', countryCode: 23 }]),
    } as unknown as ApiService;
    let receivedError: unknown;

    new TaxJurisdictionService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid records');
  });
});
