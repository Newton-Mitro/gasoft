import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import {
  SalesCreditNote,
  SalesCreditNotePayload,
  SalesCreditNotePostingPayload,
  CustomerRefundPayload,
} from './sales-credit-note.model';

const ENDPOINT = 'sales-credit-notes';

@Injectable({ providedIn: 'root' })
export class SalesCreditNotesService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<SalesCreditNote[]> {
    return this.api.get<SalesCreditNote[]>(ENDPOINT, true);
  }

  create(payload: SalesCreditNotePayload): Observable<SalesCreditNote> {
    return this.api.post<SalesCreditNote>(ENDPOINT, payload, true);
  }

  post(id: number, payload: SalesCreditNotePostingPayload): Observable<SalesCreditNote> {
    return this.api.post<SalesCreditNote>(`${ENDPOINT}/${id}/post`, payload, true);
  }

  refund(id: number, payload: CustomerRefundPayload): Observable<unknown> {
    return this.api.post<unknown>(`${ENDPOINT}/${id}/refunds`, payload, true);
  }
}
