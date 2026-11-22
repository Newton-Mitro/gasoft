import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PosService } from './pos.service';

describe('PosService', () => {
  it('loads and saves company-scoped POS defaults', () => {
    const calls: unknown[][] = [];
    const settings = {
      warehouseId: 1,
      journalId: 2,
      accountingPeriodId: 3,
      revenueAccountId: 4,
      receivableAccountId: 5,
      settlementAccountId: 6,
      taxCodeId: null,
      taxAccountId: null,
      inventoryAccountId: 7,
      costOfGoodsSoldAccountId: 8,
    };
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(settings);
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of(settings);
      },
    } as unknown as ApiService;
    const service = new PosService(api);

    service.getSettings().subscribe();
    service.saveSettings(settings).subscribe();

    expect(calls).toEqual([
      ['GET', 'pos/settings', true],
      ['PUT', 'pos/settings', settings, true],
    ]);
  });

  it('submits checkout through the company-scoped POS endpoint', () => {
    const calls: unknown[][] = [];
    const result = {
      invoice: {
        id: 12,
        invoiceNumber: 'POS-12',
        grandTotal: '25.00',
        currencyCode: 'USD',
        lines: [],
      },
      payment: { id: 8, paymentNumber: 'POS-PAY-12', status: 'Posted' },
    };
    const api = {
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push([endpoint, body, companyScoped]);
        return of(result);
      },
    } as unknown as ApiService;
    const payload = {
      partyId: 7,
      invoiceNumber: 'POS-12',
      paymentNumber: 'POS-PAY-12',
      saleDate: '2026-11-21',
      currencyCode: 'USD',
      exchangeRate: 1,
      lines: [],
      warehouseId: 1,
      journalId: 2,
      accountingPeriodId: 3,
      receivableAccountId: 4,
      taxAccounts: [],
      settlementAccountId: 5,
      inventoryAccountId: 6,
      costOfGoodsSoldAccountId: 7,
    };

    let response: unknown;
    new PosService(api).checkout(payload).subscribe((value) => (response = value));

    expect(response).toEqual(result);
    expect(calls).toEqual([['pos/checkout', payload, true]]);
  });
});
