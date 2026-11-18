import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import {
  PaymentAllocationPayload,
  PaymentPostingPayload,
  PurchaseBillPostingPayload,
  SalesInvoicePostingPayload,
} from './business.models';

export type BusinessActionResult = Record<string, unknown>;

@Injectable({ providedIn: 'root' })
export class BusinessService {
  constructor(private readonly api: ApiService) {}

  postSalesInvoice(id: number, payload: SalesInvoicePostingPayload): Observable<BusinessActionResult> {
    return this.api.post<BusinessActionResult>(`sales-invoices/${id}/post`, payload, true);
  }

  postPurchaseBill(id: number, payload: PurchaseBillPostingPayload): Observable<BusinessActionResult> {
    return this.api.post<BusinessActionResult>(`purchase-bills/${id}/post`, payload, true);
  }

  postPayment(id: number, payload: PaymentPostingPayload): Observable<BusinessActionResult> {
    return this.api.post<BusinessActionResult>(`payments/${id}/post`, payload, true);
  }

  allocatePayment(id: number, payload: PaymentAllocationPayload): Observable<BusinessActionResult> {
    return this.api.post<BusinessActionResult>(`payments/${id}/allocations`, payload, true);
  }
}
