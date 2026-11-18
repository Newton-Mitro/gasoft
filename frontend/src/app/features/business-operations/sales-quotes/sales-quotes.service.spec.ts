import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { SalesQuotesService } from './sales-quotes.service';

describe('SalesQuotesService', () => {
  it('uses company-scoped endpoints for quote lifecycle operations', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of([]);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of({});
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of({});
      },
    } as unknown as ApiService;
    const service = new SalesQuotesService(api);
    const payload = {
      customerId: 3,
      quoteNumber: 'Q-100',
      quoteDate: '2026-11-16',
      validUntil: '2026-12-16',
      currencyCode: 'USD',
      exchangeRate: 1,
      lines: [{ accountId: 8, taxCodeId: null, description: 'Service', quantity: 1, unitPrice: 100 }],
    };

    service.getAll().subscribe();
    service.create(payload).subscribe();
    service.update(2, payload).subscribe();
    service.sendQuote(2).subscribe();
    service.transition(2, 'accept').subscribe();
    service.convert(2, 'INV-Q-100', '2026-11-16', '2026-12-16').subscribe();

    expect(calls).toEqual([
      ['GET', 'sales-quotes', true],
      ['POST', 'sales-quotes', payload, true],
      ['PUT', 'sales-quotes/2', payload, true],
      ['POST', 'sales-quotes/2/send', {}, true],
      ['POST', 'sales-quotes/2/accept', {}, true],
      ['POST', 'sales-quotes/2/convert', {
        invoiceNumber: 'INV-Q-100',
        invoiceDate: '2026-11-16',
        dueDate: '2026-12-16',
      }, true],
    ]);
  });
});
