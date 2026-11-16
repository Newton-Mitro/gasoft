import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { ItemCategory, ItemCategoryPayload } from './item-category.model';

const ENDPOINT = '/api/v1/item-categories';

@Injectable({ providedIn: 'root' })
export class ItemCategoryService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<ItemCategory[]> {
    return this.api.get<unknown>(ENDPOINT, false).pipe(map(toItemCategories));
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<ItemCategory>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>(`${ENDPOINT}/page`, false, params)
      .pipe(map(result => parsePagedResult(result, isItemCategory, 'item categories')));
  }

  getById(id: number): Observable<ItemCategory> {
    return this.api.get<ItemCategory>(`${ENDPOINT}/${id}`, false);
  }

  create(payload: ItemCategoryPayload): Observable<ItemCategory> {
    return this.api.post<ItemCategory>(ENDPOINT, payload, false);
  }

  update(id: number, payload: ItemCategoryPayload): Observable<ItemCategory> {
    return this.api.put<ItemCategory>(`${ENDPOINT}/${id}`, payload, false);
  }
}

function toItemCategories(result: unknown): ItemCategory[] {
  if (!Array.isArray(result)) throw new Error('The item categories response has an unsupported format.');
  if (!result.every(isItemCategory)) throw new Error('The item categories response contains an invalid record.');
  return result;
}

function isItemCategory(value: unknown): value is ItemCategory {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    typeof row['code'] === 'string' &&
    typeof row['name'] === 'string';
}
