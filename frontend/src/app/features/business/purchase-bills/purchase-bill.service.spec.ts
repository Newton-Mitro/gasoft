import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PurchaseBillService } from './purchase-bill.service';

describe('PurchaseBillService', () => {
  it('uses company-scoped bill endpoints for CRUD', () => {
    const calls: unknown[][] = [];
    const bill = {
      id: 8,
      partyId: 7,
      billNumber: 'BILL-8',
      billDate: '2026-11-15',
      dueDate: '2026-12-15',
      currencyCode: 'USD',
      exchangeRate: '1.000000',
      grandTotal: '25.00',
      allocatedTotal: '0.00',
      status: 'Draft',
      lines: [{ accountId: 9, taxCodeId: null, description: 'Materials', quantity: '1', unitPrice: '25' }],
    };
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(endpoint.endsWith('/8') ? bill : { items: [bill] });
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of(bill);
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of(bill);
      },
    } as unknown as ApiService;
    const service = new PurchaseBillService(api);
    const payload = {
      partyId: 7,
      billNumber: 'BILL-8',
      billDate: '2026-11-15',
      dueDate: '2026-12-15',
      currencyCode: 'USD',
      exchangeRate: 1,
      lines: [{ accountId: 9, taxCodeId: null, description: 'Materials', quantity: 1, unitPrice: 25 }],
    };

    service.getAll().subscribe();
    service.getById(8).subscribe();
    service.create(payload).subscribe();
    service.update(8, payload).subscribe();

    expect(calls).toEqual([
      ['GET', 'purchase-bills', true],
      ['GET', 'purchase-bills/8', true],
      ['POST', 'purchase-bills', payload, true],
      ['PUT', 'purchase-bills/8', payload, true],
    ]);
  });

  it('allows summary-list records without line details', () => {
    const bill = {
      id: 8,
      partyId: 7,
      billNumber: 'BILL-8',
      billDate: '2026-11-15',
      dueDate: '2026-12-15',
      currencyCode: 'USD',
      exchangeRate: 1,
      grandTotal: 25,
      allocatedTotal: 0,
      status: 'Draft',
    };
    const api = ({ get: () => of({ items: [bill] }) }) as unknown as ApiService;
    let rows: unknown;

    new PurchaseBillService(api).getAll().subscribe(result => rows = result);

    expect(rows).toEqual([bill]);
  });

  it('rejects malformed bill line records', () => {
    const api = ({
      get: () => of([{
        id: 8,
        partyId: 7,
        billNumber: 'BILL-8',
        billDate: '2026-11-15',
        dueDate: '2026-12-15',
        currencyCode: 'USD',
        exchangeRate: 1,
        grandTotal: 25,
        allocatedTotal: 0,
        status: 'Draft',
        lines: [{ accountId: 9, taxCodeId: null, description: 'Materials', quantity: 'bad', unitPrice: 25 }],
      }]),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new PurchaseBillService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
