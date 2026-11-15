import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PurchaseBill, PurchaseBillPayload } from './purchase-bill.model';

const ENDPOINT = 'purchase-bills';

@Injectable({ providedIn: 'root' })
export class PurchaseBillService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<PurchaseBill[]> {
    return this.api.get<unknown>(ENDPOINT, true).pipe(map(toPurchaseBills));
  }

  getById(id: number): Observable<PurchaseBill> {
    return this.api.get<PurchaseBill>(`${ENDPOINT}/${id}`, true);
  }

  create(payload: PurchaseBillPayload): Observable<PurchaseBill> {
    return this.api.post<PurchaseBill>(ENDPOINT, payload, true);
  }

  update(id: number, payload: PurchaseBillPayload): Observable<PurchaseBill> {
    return this.api.put<PurchaseBill>(`${ENDPOINT}/${id}`, payload, true);
  }
}

function toPurchaseBills(result: unknown): PurchaseBill[] {
  const rows = Array.isArray(result)
    ? result
    : result && typeof result === 'object' && Array.isArray((result as Record<string, unknown>)['items'])
      ? (result as Record<string, unknown>)['items'] as unknown[]
      : null;
  if (!rows) throw new Error('The purchase bills response has an unsupported format.');
  if (!rows.every(isPurchaseBill)) throw new Error('The purchase bills response contains an invalid record.');
  return rows;
}

function isPurchaseBill(value: unknown): value is PurchaseBill {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    Number.isSafeInteger(row['partyId']) &&
    typeof row['billNumber'] === 'string' &&
    typeof row['billDate'] === 'string' &&
    typeof row['dueDate'] === 'string' &&
    typeof row['currencyCode'] === 'string' &&
    isNumericValue(row['exchangeRate']) &&
    isNumericValue(row['grandTotal']) &&
    isNumericValue(row['allocatedTotal']) &&
    typeof row['status'] === 'string' &&
    (row['lines'] === undefined ||
      (Array.isArray(row['lines']) && row['lines'].every(isBillLine)));
}

function isBillLine(value: unknown): boolean {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['accountId']) &&
    (row['taxCodeId'] === null || Number.isSafeInteger(row['taxCodeId'])) &&
    typeof row['description'] === 'string' &&
    isNumericValue(row['quantity']) &&
    isNumericValue(row['unitPrice']);
}

function isNumericValue(value: unknown): value is number | string {
  return (typeof value === 'number' && Number.isFinite(value)) ||
    (typeof value === 'string' && value.trim() !== '' && Number.isFinite(Number(value)));
}
