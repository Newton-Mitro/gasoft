import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { InventoryMovement } from './inventory-movement.model';

@Injectable({ providedIn: 'root' })
export class InventoryMovementService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<InventoryMovement[]> {
    return this.api.get<unknown>('inventory/movements', true).pipe(map(toMovements));
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<InventoryMovement>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>('inventory/movements/page', true, params)
      .pipe(map(result => parsePagedResult(result, isMovement, 'stock movements')));
  }
}

function toMovements(result: unknown): InventoryMovement[] {
  const rows = Array.isArray(result)
    ? result
    : result && typeof result === 'object' && Array.isArray((result as Record<string, unknown>)['items'])
      ? (result as Record<string, unknown>)['items'] as unknown[]
      : null;
  if (!rows) throw new Error('The stock movements response has an unsupported format.');
  if (!rows.every(isMovement)) throw new Error('The stock movements response contains an invalid record.');
  return rows;
}

function isMovement(value: unknown): value is InventoryMovement {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    typeof row['movementNumber'] === 'string' &&
    typeof row['movementType'] === 'string' &&
    typeof row['movementDate'] === 'string' &&
    isScalar(row['itemId']) &&
    isScalar(row['warehouseId']) &&
    isScalar(row['quantityIn']) &&
    isScalar(row['quantityOut']) &&
    isScalar(row['totalCost']);
}

function isScalar(value: unknown): value is number | string {
  return typeof value === 'string' || (typeof value === 'number' && Number.isFinite(value));
}
