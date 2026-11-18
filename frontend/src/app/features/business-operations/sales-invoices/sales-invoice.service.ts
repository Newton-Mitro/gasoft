import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { SalesInvoice, SalesInvoicePayload } from './sales-invoice.model';

const ENDPOINT = 'sales-invoices';

@Injectable({ providedIn: 'root' })
export class SalesInvoiceService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<SalesInvoice[]> {
    return this.api.get<unknown>(ENDPOINT, true).pipe(map(toSalesInvoices));
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<SalesInvoice>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>(`${ENDPOINT}/page`, true, params)
      .pipe(map(result => parsePagedResult(result, isSalesInvoice, 'sales invoices')));
  }

  getById(id: number): Observable<SalesInvoice> {
    return this.api.get<SalesInvoice>(`${ENDPOINT}/${id}`, true);
  }

  create(payload: SalesInvoicePayload): Observable<SalesInvoice> {
    return this.api.post<SalesInvoice>(ENDPOINT, payload, true);
  }

  update(id: number, payload: SalesInvoicePayload): Observable<SalesInvoice> {
    return this.api.put<SalesInvoice>(`${ENDPOINT}/${id}`, payload, true);
  }
}

function toSalesInvoices(result: unknown): SalesInvoice[] {
  const rows = Array.isArray(result)
    ? result
    : result && typeof result === 'object' && Array.isArray((result as Record<string, unknown>)['items'])
      ? (result as Record<string, unknown>)['items'] as unknown[]
      : null;
  if (!rows) throw new Error('The sales invoices response has an unsupported format.');
  if (!rows.every(isSalesInvoice)) throw new Error('The sales invoices response contains an invalid record.');
  return rows;
}

function isSalesInvoice(value: unknown): value is SalesInvoice {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    Number.isSafeInteger(row['partyId']) &&
    typeof row['invoiceNumber'] === 'string' &&
    typeof row['invoiceDate'] === 'string' &&
    typeof row['dueDate'] === 'string' &&
    typeof row['currencyCode'] === 'string' &&
    isNumericValue(row['exchangeRate']) &&
    isNumericValue(row['grandTotal']) &&
    isNumericValue(row['allocatedTotal']) &&
    typeof row['status'] === 'string' &&
    (row['lines'] === undefined ||
      (Array.isArray(row['lines']) && row['lines'].every(isInvoiceLine)));
}

function isInvoiceLine(value: unknown): boolean {
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
