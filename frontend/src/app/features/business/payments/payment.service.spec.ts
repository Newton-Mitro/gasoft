import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PaymentService } from './payment.service';

describe('PaymentService', () => {
  const payment = {
    id: 9,
    partyId: 7,
    paymentNumber: 'PAY-9',
    direction: 'Receive',
    paymentDate: '2026-11-15',
    currencyCode: 'USD',
    exchangeRate: '1.000000',
    amount: '25.00',
    allocatedAmount: '0.00',
    status: 'Draft',
  };

  it('uses company-scoped endpoints for CRUD', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(endpoint.endsWith('/9') ? payment : { items: [payment] });
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of(payment);
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of(payment);
      },
    } as unknown as ApiService;
    const service = new PaymentService(api);
    const payload = {
      partyId: 7,
      paymentNumber: 'PAY-9',
      direction: 'Receive' as const,
      paymentDate: '2026-11-15',
      currencyCode: 'USD',
      exchangeRate: 1,
      amount: 25,
    };

    service.getAll().subscribe();
    service.getById(9).subscribe();
    service.create(payload).subscribe();
    service.update(9, payload).subscribe();

    expect(calls).toEqual([
      ['GET', 'payments', true],
      ['GET', 'payments/9', true],
      ['POST', 'payments', payload, true],
      ['PUT', 'payments/9', payload, true],
    ]);
  });

  it('rejects malformed payment records', () => {
    const api = ({
      get: () => of({ items: [{ ...payment, amount: 'not-a-number' }] }),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new PaymentService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
