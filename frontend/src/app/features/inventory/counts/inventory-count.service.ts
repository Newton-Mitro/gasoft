import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { InventoryCount, InventoryCountItem, InventoryCountPayload } from './inventory-count.model';

const ENDPOINT = 'inventory/counts';

@Injectable({ providedIn: 'root' })
export class InventoryCountService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<InventoryCount[]> {
    return this.api.get<unknown>(ENDPOINT, true).pipe(map(toInventoryCounts));
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<InventoryCount>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>(`${ENDPOINT}/page`, true, params)
      .pipe(map(result => parsePagedResult(result, isInventoryCount, 'inventory counts')));
  }

  getById(id: number): Observable<InventoryCount> {
    return this.api.get<InventoryCount>(`${ENDPOINT}/${id}`, true);
  }

  create(payload: InventoryCountPayload): Observable<InventoryCount> {
    return this.api.post<InventoryCount>(ENDPOINT, payload, true);
  }

  update(id: number, payload: InventoryCountPayload): Observable<InventoryCount> {
    return this.api.put<InventoryCount>(`${ENDPOINT}/${id}`, payload, true);
  }
}

function toInventoryCounts(result: unknown): InventoryCount[] {
  const rows = Array.isArray(result)
    ? result
    : result && typeof result === 'object' && Array.isArray((result as Record<string, unknown>)['items'])
      ? (result as Record<string, unknown>)['items'] as unknown[]
      : null;
  if (!rows) throw new Error('The inventory counts response has an unsupported format.');
  if (!rows.every(isInventoryCount)) throw new Error('The inventory counts response contains an invalid record.');
  return rows;
}

function isInventoryCount(value: unknown): value is InventoryCount {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    typeof row['countNumber'] === 'string' &&
    typeof row['countDate'] === 'string' &&
    Number.isSafeInteger(row['warehouseId']) &&
    typeof row['status'] === 'string' &&
    Array.isArray(row['items']) &&
    row['items'].every(isInventoryCountItem);
}

function isInventoryCountItem(value: unknown): value is InventoryCountItem {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['itemId']) &&
    isNumericValue(row['countedQuantity']) &&
    (row['adjustmentUnitCost'] === null ||
      isNumericValue(row['adjustmentUnitCost']));
}

function isNumericValue(value: unknown): value is number | string {
  return (typeof value === 'number' && Number.isFinite(value)) ||
    (typeof value === 'string' && value.trim() !== '' && Number.isFinite(Number(value)));
}
