import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import {
  PurchaseOrder,
  PurchaseOrderPayload,
  PurchaseOrderReceipt,
  ReceivePurchaseOrderLinePayload,
} from './purchase-orders.model';

const ENDPOINT = 'purchase-orders';

@Injectable({ providedIn: 'root' })
export class PurchaseOrdersService {
  constructor(private readonly api: ApiService) {}

  getOrders(): Observable<PurchaseOrder[]> {
    return this.api.get<unknown>(ENDPOINT).pipe(map(value => {
      if (!Array.isArray(value) || !value.every(isPurchaseOrder)) {
        throw new Error('The purchase orders response contains invalid records.');
      }
      return value;
    }));
  }

  getOrdersPage(page: number, pageSize: number): Observable<PagedResult<PurchaseOrder>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>(`${ENDPOINT}/page`, true, params)
      .pipe(map(value => parsePagedResult(value, isPurchaseOrder, 'purchase orders')));
  }

  getOrder(orderId: number): Observable<PurchaseOrder> {
    return this.api.get<unknown>(`${ENDPOINT}/${orderId}`).pipe(map(toPurchaseOrder));
  }

  create(payload: PurchaseOrderPayload): Observable<PurchaseOrder> {
    return this.api.post<unknown>(ENDPOINT, payload).pipe(map(toPurchaseOrder));
  }

  update(orderId: number, payload: PurchaseOrderPayload): Observable<PurchaseOrder> {
    return this.api.put<unknown>(`${ENDPOINT}/${orderId}`, payload).pipe(map(toPurchaseOrder));
  }

  place(orderId: number, revision: number): Observable<PurchaseOrder> {
    return this.api.post<unknown>(`${ENDPOINT}/${orderId}/order`, { revision }).pipe(map(toPurchaseOrder));
  }

  cancel(orderId: number, revision: number): Observable<unknown> {
    return this.api.post<unknown>(`${ENDPOINT}/${orderId}/cancel`, { revision });
  }

  receive(
    orderId: number,
    lineId: number,
    payload: ReceivePurchaseOrderLinePayload,
  ): Observable<PurchaseOrderReceipt> {
    return this.api.post<unknown>(`${ENDPOINT}/${orderId}/lines/${lineId}/receive`, payload)
      .pipe(map(toReceipt));
  }
}

function toPurchaseOrder(value: unknown): PurchaseOrder {
  if (!isPurchaseOrder(value)) throw new Error('The purchase order response contains an invalid record.');
  return value;
}

function isPurchaseOrder(value: unknown): value is PurchaseOrder {
  if (!isRecord(value)) return false;
  return isPositiveInteger(value['id']) &&
    isPositiveInteger(value['companyId']) &&
    isPositiveInteger(value['supplierId']) &&
    typeof value['supplierName'] === 'string' &&
    isPositiveInteger(value['warehouseId']) &&
    typeof value['orderNumber'] === 'string' &&
    isDate(value['orderDate']) &&
    (value['expectedDate'] === null || isDate(value['expectedDate'])) &&
    isCurrency(value['currencyCode']) &&
    isNumeric(value['exchangeRate']) &&
    typeof value['status'] === 'string' &&
    isNonNegativeInteger(value['revision']) &&
    Array.isArray(value['lines']) &&
    value['lines'].every(isLine);
}

function isLine(value: unknown): boolean {
  if (!isRecord(value)) return false;
  return isPositiveInteger(value['id']) &&
    isPositiveInteger(value['itemId']) &&
    typeof value['sku'] === 'string' &&
    typeof value['itemName'] === 'string' &&
    typeof value['unitCode'] === 'string' &&
    typeof value['description'] === 'string' &&
    isNumeric(value['quantity']) &&
    isNumeric(value['receivedQuantity']) &&
    isNumeric(value['unitCost']);
}

function toReceipt(value: unknown): PurchaseOrderReceipt {
  if (!isPurchaseOrderReceipt(value)) {
    throw new Error('The purchase order receipt response is invalid.');
  }
  return value;
}

function isPurchaseOrderReceipt(value: unknown): value is PurchaseOrderReceipt {
  if (!isRecord(value) || !isRecord(value['movement']) ||
    !isPositiveInteger(value['movement']['id']) ||
    typeof value['movement']['movementNumber'] !== 'string' ||
    !isDate(value['movement']['movementDate']) ||
    !isNumeric(value['movement']['quantityIn']) ||
    !isNumeric(value['movement']['unitCost']) ||
    !isNumeric(value['movement']['totalCost']) ||
    typeof value['purchaseOrderStatus'] !== 'string') return false;
  return true;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value && typeof value === 'object' && !Array.isArray(value));
}

function isPositiveInteger(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

function isNonNegativeInteger(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value >= 0;
}

function isNumeric(value: unknown): value is number {
  return typeof value === 'number' && Number.isFinite(value);
}

function isCurrency(value: unknown): value is string {
  return typeof value === 'string' && /^[A-Z]{3}$/.test(value);
}

function isDate(value: unknown): value is string {
  return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value);
}
