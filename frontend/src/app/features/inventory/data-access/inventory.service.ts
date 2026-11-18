import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import {
  InventoryCountPostingPayload,
  InventoryIssuePayload,
  InventoryReceiptPayload,
  WarehouseLocationPayload,
} from './inventory.models';

export type InventoryActionResult = Record<string, unknown>;

@Injectable({ providedIn: 'root' })
export class InventoryService {
  constructor(private readonly api: ApiService) {}

  recordReceipt(payload: InventoryReceiptPayload): Observable<InventoryActionResult> {
    return this.api.post<InventoryActionResult>('inventory/receipts', payload, true);
  }

  recordIssue(payload: InventoryIssuePayload): Observable<InventoryActionResult> {
    return this.api.post<InventoryActionResult>('inventory/issues', payload, true);
  }

  postCount(id: number, payload: InventoryCountPostingPayload): Observable<InventoryActionResult> {
    return this.api.post<InventoryActionResult>(`inventory/counts/${id}/post`, payload, true);
  }

  createLocation(warehouseId: number, payload: WarehouseLocationPayload): Observable<InventoryActionResult> {
    return this.api.post<InventoryActionResult>(`warehouses/${warehouseId}/locations`, payload, true);
  }
}
