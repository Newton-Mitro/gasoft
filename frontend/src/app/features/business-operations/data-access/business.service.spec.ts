import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { BusinessService } from './business.service';

describe('BusinessService', () => {
  it('posts invoices and allocations through company-scoped endpoints', () => {
    const calls: unknown[][] = [];
    const api = {
      post: (endpoint: string, payload: unknown, companyScoped: boolean) => {
        calls.push([endpoint, payload, companyScoped]);
        return of({ id: 5 });
      },
    } as unknown as ApiService;
    const service = new BusinessService(api);
    const posting = {
      journalId: 2,
      accountingPeriodId: 3,
      receivableAccountId: 4,
      taxAccounts: [{ taxCodeId: 6, accountId: 7 }],
    };
    const allocation = { amount: 25, salesInvoiceId: 8, purchaseBillId: null };

    service.postSalesInvoice(5, posting).subscribe();
    service.allocatePayment(9, allocation).subscribe();

    expect(calls).toEqual([
      ['sales-invoices/5/post', posting, true],
      ['payments/9/allocations', allocation, true],
    ]);
  });
});
