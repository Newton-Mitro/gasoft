import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { ExpenseReportsService } from './expense-reports.service';

describe('ExpenseReportsService', () => {
  it('uses company-scoped endpoints for the approval and accounting workflow', () => {
    const calls: unknown[][] = [];
    const report = { id: 12 };
    const api = {
      get: (endpoint: string, companyScoped = true) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of([report]);
      },
      put: (endpoint: string, body: unknown, companyScoped = true) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of(report);
      },
      post: (endpoint: string, body: unknown, companyScoped = true) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of(report);
      },
    } as unknown as ApiService;
    const service = new ExpenseReportsService(api);
    const payload = { title: 'Client visit', lines: [] };

    service.getAll().subscribe();
    service.create(payload).subscribe();
    service.update(12, payload).subscribe();
    service.submit(12).subscribe();
    service.approve(12, null).subscribe();
    service.reject(12, 'Missing details').subscribe();
    service.post(12, 2, 3, 4).subscribe();
    service.reimburse(12, 2, 3, 5).subscribe();

    expect(calls).toEqual([
      ['GET', 'expense-reports', true],
      ['POST', 'expense-reports', payload, true],
      ['PUT', 'expense-reports/12', payload, true],
      ['POST', 'expense-reports/12/submit', {}, true],
      ['POST', 'expense-reports/12/approve', { note: null }, true],
      ['POST', 'expense-reports/12/reject', { note: 'Missing details' }, true],
      [
        'POST',
        'expense-reports/12/post',
        {
          journalId: 2,
          accountingPeriodId: 3,
          employeePayableAccountId: 4,
        },
        true,
      ],
      [
        'POST',
        'expense-reports/12/reimburse',
        {
          journalId: 2,
          accountingPeriodId: 3,
          cashAccountId: 5,
        },
        true,
      ],
    ]);
  });

  it('uploads receipts as multipart form data and fetches them through the authenticated API client', () => {
    let uploadBody: unknown;
    let receiptPath = '';
    let receiptScoped: boolean | undefined;
    const file = new File(['receipt'], 'receipt.pdf', { type: 'application/pdf' });
    const api = {
      post: (_endpoint: string, body: unknown) => {
        uploadBody = body;
        return of({ id: 12 });
      },
      getBlob: (path: string, companyScoped: boolean) => {
        receiptPath = path;
        receiptScoped = companyScoped;
        return of(new Blob(['receipt']));
      },
    } as unknown as ApiService;
    const service = new ExpenseReportsService(api);
    const receiptUrl = '/api/v1/companies/2/expense-reports/12/lines/9/receipt/file.pdf';

    service.uploadReceipt(12, 9, file).subscribe();
    service.getReceipt(receiptUrl).subscribe();

    expect(uploadBody).toBeInstanceOf(FormData);
    expect((uploadBody as FormData).get('receipt')).toBe(file);
    expect(receiptPath).toBe(receiptUrl);
    expect(receiptScoped).toBe(false);
  });
});
