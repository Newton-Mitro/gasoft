import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { SalesQuote, SalesQuotePayload } from './sales-quote.model';

const ENDPOINT = 'sales-quotes';

@Injectable({ providedIn: 'root' })
export class SalesQuotesService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<SalesQuote[]> {
    return this.api.get<SalesQuote[]>(ENDPOINT, true);
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<SalesQuote>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>(`${ENDPOINT}/page`, true, params)
      .pipe(map(result => parsePagedResult(result, isQuote, 'sales quotes')));
  }

  getById(id: number): Observable<SalesQuote> {
    return this.api.get<SalesQuote>(`${ENDPOINT}/${id}`, true);
  }

  create(payload: SalesQuotePayload): Observable<SalesQuote> {
    return this.api.post<SalesQuote>(ENDPOINT, payload, true);
  }

  update(id: number, payload: SalesQuotePayload): Observable<SalesQuote> {
    return this.api.put<SalesQuote>(`${ENDPOINT}/${id}`, payload, true);
  }

  transition(id: number, action: 'accept' | 'decline' | 'cancel'): Observable<unknown> {
    return this.api.post(`${ENDPOINT}/${id}/${action}`, {}, true);
  }

  sendQuote(id: number): Observable<{ id: number; status: string; sentAt: string; sentTo: string }> {
    return this.api.post(`${ENDPOINT}/${id}/send`, {}, true);
  }

  convert(id: number, invoiceNumber: string, invoiceDate: string, dueDate: string): Observable<unknown> {
    return this.api.post(`${ENDPOINT}/${id}/convert`, { invoiceNumber, invoiceDate, dueDate }, true);
  }
}

function isQuote(value: unknown): value is SalesQuote {
  return Boolean(value && typeof value === 'object' &&
    Number.isSafeInteger((value as Record<string, unknown>)['id']) &&
    typeof (value as Record<string, unknown>)['quoteNumber'] === 'string');
}
