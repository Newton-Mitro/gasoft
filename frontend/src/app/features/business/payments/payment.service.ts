import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { Payment, PaymentPayload } from './payment.model';

const ENDPOINT = 'payments';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<Payment[]> {
    return this.api.get<unknown>(ENDPOINT, true).pipe(map(toPayments));
  }

  getById(id: number): Observable<Payment> {
    return this.api.get<unknown>(`${ENDPOINT}/${id}`, true).pipe(map(toPayment));
  }

  create(payload: PaymentPayload): Observable<Payment> {
    return this.api.post<unknown>(ENDPOINT, payload, true).pipe(map(toPayment));
  }

  update(id: number, payload: PaymentPayload): Observable<Payment> {
    return this.api.put<unknown>(`${ENDPOINT}/${id}`, payload, true).pipe(map(toPayment));
  }
}

function toPayments(result: unknown): Payment[] {
  const rows = Array.isArray(result)
    ? result
    : result && typeof result === 'object' && Array.isArray((result as Record<string, unknown>)['items'])
      ? (result as Record<string, unknown>)['items'] as unknown[]
      : null;
  if (!rows) throw new Error('The payments response has an unsupported format.');
  if (!rows.every(isPayment)) throw new Error('The payments response contains an invalid record.');
  return rows;
}

function toPayment(result: unknown): Payment {
  if (!isPayment(result)) throw new Error('The payment response contains an invalid record.');
  return result;
}

function isPayment(value: unknown): value is Payment {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    Number.isSafeInteger(row['partyId']) &&
    typeof row['paymentNumber'] === 'string' &&
    (row['direction'] === 'Receive' || row['direction'] === 'Pay') &&
    typeof row['paymentDate'] === 'string' &&
    typeof row['currencyCode'] === 'string' &&
    isNumericValue(row['exchangeRate']) &&
    isNumericValue(row['amount']) &&
    isNumericValue(row['allocatedAmount']) &&
    typeof row['status'] === 'string';
}

function isNumericValue(value: unknown): value is number | string {
  return (typeof value === 'number' && Number.isFinite(value)) ||
    (typeof value === 'string' && value.trim() !== '' && Number.isFinite(Number(value)));
}
