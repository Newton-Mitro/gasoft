import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { WarehouseLocationPayload } from '../data-access/inventory.models';
import { Warehouse, WarehouseLocation, WarehousePayload } from './warehouse.model';

const ENDPOINT = 'warehouses';

@Injectable({ providedIn: 'root' })
export class WarehouseService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<Warehouse[]> {
    return this.api.get<unknown>(ENDPOINT, true).pipe(map(toWarehouses));
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<Warehouse>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>(`${ENDPOINT}/page`, true, params)
      .pipe(map(result => parsePagedResult(result, isWarehouse, 'warehouses')));
  }

  getById(id: number): Observable<Warehouse> {
    return this.api.get<Warehouse>(`${ENDPOINT}/${id}`, true);
  }

  getLocations(warehouseId: number): Observable<WarehouseLocation[]> {
    return this.api.get<unknown>(`${ENDPOINT}/${warehouseId}/locations`, true)
      .pipe(map(toWarehouseLocations));
  }

  getLocation(warehouseId: number, locationId: number): Observable<WarehouseLocation> {
    return this.api.get<unknown>(`${ENDPOINT}/${warehouseId}/locations/${locationId}`, true)
      .pipe(map(toWarehouseLocation));
  }

  updateLocation(
    warehouseId: number,
    locationId: number,
    payload: WarehouseLocationPayload,
  ): Observable<WarehouseLocation> {
    return this.api.put<unknown>(
      `${ENDPOINT}/${warehouseId}/locations/${locationId}`,
      payload,
      true,
    ).pipe(map(toWarehouseLocation));
  }

  create(payload: WarehousePayload): Observable<Warehouse> {
    return this.api.post<Warehouse>(ENDPOINT, payload, true);
  }

  update(id: number, payload: WarehousePayload): Observable<Warehouse> {
    return this.api.put<Warehouse>(`${ENDPOINT}/${id}`, payload, true);
  }
}

function toWarehouses(result: unknown): Warehouse[] {
  if (!Array.isArray(result)) throw new Error('The warehouses response has an unsupported format.');
  if (!result.every(isWarehouse)) throw new Error('The warehouses response contains an invalid record.');
  return result;
}

function toWarehouseLocations(result: unknown): WarehouseLocation[] {
  if (!Array.isArray(result)) throw new Error('The warehouse locations response has an unsupported format.');
  if (!result.every(isWarehouseLocation)) {
    throw new Error('The warehouse locations response contains an invalid record.');
  }
  return result;
}

function toWarehouseLocation(result: unknown): WarehouseLocation {
  if (!isWarehouseLocation(result)) {
    throw new Error('The warehouse location response contains an invalid record.');
  }
  return result;
}

function isWarehouse(value: unknown): value is Warehouse {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    typeof row['code'] === 'string' &&
    typeof row['name'] === 'string';
}

function isWarehouseLocation(value: unknown): value is WarehouseLocation {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    Number.isSafeInteger(row['warehouseId']) &&
    typeof row['code'] === 'string' &&
    typeof row['name'] === 'string';
}
