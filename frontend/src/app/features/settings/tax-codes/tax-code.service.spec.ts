import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { TaxCodeService } from './tax-code.service';

describe('TaxCodeService', () => {
  const rate = { id: 9, taxCodeId: 4, ratePercent: 15, validFrom: '2026-01-01', validTo: null };
  const taxCode = {
    id: 4,
    jurisdictionId: 2,
    jurisdictionCode: 'BD',
    countryCode: 'BD',
    categoryId: 1,
    categoryCode: 'STANDARD',
    categoryName: 'Standard',
    code: 'VAT15',
    taxType: 'VAT',
    rates: [rate],
  };

  it('uses company-scoped tax-code and rate endpoints', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string) => {
        calls.push(['GET', endpoint]);
        return of([taxCode]);
      },
      post: (endpoint: string, body: unknown) => {
        calls.push(['POST', endpoint, body]);
        return of(endpoint.endsWith('/rates') ? rate : taxCode);
      },
      put: (endpoint: string, body: unknown) => {
        calls.push(['PUT', endpoint, body]);
        return of(endpoint.includes('/rates/') ? rate : taxCode);
      },
      delete: (endpoint: string) => {
        calls.push(['DELETE', endpoint]);
        return of(undefined);
      },
    } as unknown as ApiService;
    const service = new TaxCodeService(api);
    const taxCodePayload = { jurisdictionId: 2, categoryId: 1, code: 'VAT15', taxType: 'VAT' };
    const ratePayload = { ratePercent: 15, validFrom: '2026-01-01', validTo: null };

    service.getAll().subscribe();
    service.create(taxCodePayload).subscribe();
    service.update(4, taxCodePayload).subscribe();
    service.delete(4).subscribe();
    service.createRate(4, ratePayload).subscribe();
    service.updateRate(4, 9, ratePayload).subscribe();
    service.deleteRate(4, 9).subscribe();

    expect(calls).toEqual([
      ['GET', 'tax-codes'],
      ['POST', 'tax-codes', taxCodePayload],
      ['PUT', 'tax-codes/4', taxCodePayload],
      ['DELETE', 'tax-codes/4'],
      ['POST', 'tax-codes/4/rates', ratePayload],
      ['PUT', 'tax-codes/4/rates/9', ratePayload],
      ['DELETE', 'tax-codes/4/rates/9'],
    ]);
  });

  it('rejects malformed tax-code responses', () => {
    const api = { get: () => of([{ ...taxCode, rates: [{ ...rate, ratePercent: '15' }] }]) } as unknown as ApiService;
    let receivedError: unknown;

    new TaxCodeService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid records');
  });
});
