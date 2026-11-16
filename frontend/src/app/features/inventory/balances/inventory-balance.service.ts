import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { InventoryBalance } from './inventory-balance.model';

@Injectable({ providedIn: 'root' })
export class InventoryBalanceService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<InventoryBalance[]> {
    return this.api.get<unknown>('inventory/balances', true).pipe(map(toBalances));
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<InventoryBalance>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>('inventory/balances/page', true, params)
      .pipe(map(result => parsePagedResult(result, isInventoryBalance, 'stock balances')));
  }
}

function toBalances(result: unknown): InventoryBalance[] {
  const rows = Array.isArray(result)
    ? result
    : result && typeof result === 'object' && Array.isArray((result as Record<string, unknown>)['items'])
      ? (result as Record<string, unknown>)['items'] as unknown[]
      : null;
  if (!rows) throw new Error('The stock balances response has an unsupported format.');
  if (!rows.every(isInventoryBalance)) {
    throw new Error('The stock balances response contains an invalid record.');
  }
  return rows;
}

function isInventoryBalance(value: unknown): value is InventoryBalance {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return isScalar(row['itemId']) &&
    isScalar(row['warehouseId']) &&
    isScalar(row['quantity']) &&
    isScalar(row['value']);
}

function isScalar(value: unknown): value is number | string {
  return typeof value === 'string' || (typeof value === 'number' && Number.isFinite(value));
}
