import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { SalesQuote, SalesQuotePayload } from './sales-quote.model';

const ENDPOINT = 'sales-quotes';

@Injectable({ providedIn: 'root' })
export class SalesQuotesService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<SalesQuote[]> {
    return this.api.get<SalesQuote[]>(ENDPOINT, true);
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
