import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { SalesInvoiceService } from './sales-invoice.service';

describe('SalesInvoiceService', () => {
  it('uses company-scoped invoice endpoints for CRUD', () => {
    const calls: unknown[][] = [];
    const invoice = {
      id: 5,
      partyId: 7,
      invoiceNumber: 'INV-5',
      invoiceDate: '2026-11-15',
      dueDate: '2026-12-15',
      currencyCode: 'USD',
      exchangeRate: '1.000000',
      grandTotal: '25.00',
      allocatedTotal: '0.00',
      status: 'Draft',
      lines: [{ accountId: 9, taxCodeId: null, description: 'Service', quantity: '1', unitPrice: '25' }],
    };
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(endpoint.endsWith('/5') ? invoice : { items: [invoice] });
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of(invoice);
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of(invoice);
      },
    } as unknown as ApiService;
    const service = new SalesInvoiceService(api);
    const payload = {
      partyId: 7,
      invoiceNumber: 'INV-5',
      invoiceDate: '2026-11-15',
      dueDate: '2026-12-15',
      currencyCode: 'USD',
      exchangeRate: 1,
      lines: [{ accountId: 9, taxCodeId: null, description: 'Service', quantity: 1, unitPrice: 25 }],
    };

    service.getAll().subscribe();
    service.getById(5).subscribe();
    service.create(payload).subscribe();
    service.update(5, payload).subscribe();

    expect(calls).toEqual([
      ['GET', 'sales-invoices', true],
      ['GET', 'sales-invoices/5', true],
      ['POST', 'sales-invoices', payload, true],
      ['PUT', 'sales-invoices/5', payload, true],
    ]);
  });

  it('allows summary-list records without line details but validates included lines', () => {
    const invoice = {
      id: 5,
      partyId: 7,
      invoiceNumber: 'INV-5',
      invoiceDate: '2026-11-15',
      dueDate: '2026-12-15',
      currencyCode: 'USD',
      exchangeRate: 1,
      grandTotal: 25,
      allocatedTotal: 0,
      status: 'Draft',
    };
    const api = ({ get: () => of({ items: [invoice] }) }) as unknown as ApiService;
    let rows: unknown;

    new SalesInvoiceService(api).getAll().subscribe(result => rows = result);

    expect(rows).toEqual([invoice]);
  });
});
