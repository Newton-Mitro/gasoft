import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { SalesCreditNotesService } from './sales-credit-notes.service';

describe('SalesCreditNotesService', () => {
  it('uses company-scoped endpoints for credit notes and refunds', () => {
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
    } as unknown as ApiService;
    const service = new SalesCreditNotesService(api);
    const payload = {
      salesInvoiceId: 7,
      creditNoteNumber: 'CN-100',
      creditNoteDate: '2026-11-16',
      reason: 'Returned goods',
      lines: [{ salesInvoiceLineId: 9, quantity: 1 }],
    };
    const posting = {
      journalId: 2,
      accountingPeriodId: 3,
      receivableAccountId: 4,
      taxAccounts: [{ taxCodeId: 5, accountId: 6 }],
    };
    const refund = {
      refundNumber: 'RF-100',
      refundDate: '2026-11-16',
      amount: 25,
      journalId: 2,
      accountingPeriodId: 3,
      cashAccountId: 7,
    };

    service.getAll().subscribe();
    service.create(payload).subscribe();
    service.post(8, posting).subscribe();
    service.refund(8, refund).subscribe();

    expect(calls).toEqual([
      ['GET', 'sales-credit-notes', true],
      ['POST', 'sales-credit-notes', payload, true],
      ['POST', 'sales-credit-notes/8/post', posting, true],
      ['POST', 'sales-credit-notes/8/refunds', refund, true],
    ]);
  });
});
