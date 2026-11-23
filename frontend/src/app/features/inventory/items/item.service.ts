import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { Item, ItemPayload } from './item.model';

@Injectable({ providedIn: 'root' })
export class ItemService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<Item[]> {
    return this.api.get<unknown>('items', true).pipe(map(toItems));
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<Item>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>('items/page', true, params)
      .pipe(map(result => parsePagedResult(result, isItem, 'inventory items')));
  }

  getById(id: number): Observable<Item> {
    return this.api.get<Item>(`items/${id}`);
  }

  create(payload: ItemPayload): Observable<Item> {
    return this.api.post<Item>('items', payload);
  }

  update(id: number, payload: ItemPayload): Observable<Item> {
    return this.api.put<Item>(`items/${id}`, payload);
  }
}

function toItems(result: unknown): Item[] {
  if (!Array.isArray(result)) throw new Error('The inventory items response has an unsupported format.');
  if (!result.every(isItem)) throw new Error('The inventory items response contains an invalid record.');
  return result;
}

function isItem(value: unknown): value is Item {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    Number.isSafeInteger(row['categoryId']) &&
    typeof row['sku'] === 'string' &&
    typeof row['name'] === 'string' &&
    typeof row['baseUnitCode'] === 'string' &&
    (row['costingMethod'] === 'FIFO' || row['costingMethod'] === 'AVCO') &&
    (row['unitSellingPrice'] === null ||
      typeof row['unitSellingPrice'] === 'number' && Number.isFinite(row['unitSellingPrice'])) &&
    (row['sellingPriceCurrencyCode'] === null ||
      typeof row['sellingPriceCurrencyCode'] === 'string');
}
