import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { TaxJurisdiction } from './tax-jurisdiction.model';

const ENDPOINT = '/api/v1/tax-jurisdictions';

@Injectable({ providedIn: 'root' })
export class TaxJurisdictionService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<TaxJurisdiction[]> {
    return this.api.get<unknown>(ENDPOINT, false).pipe(map(toTaxJurisdictions));
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<TaxJurisdiction>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>(`${ENDPOINT}/page`, false, params)
      .pipe(map(result => parsePagedResult(result, isTaxJurisdiction, 'tax jurisdictions')));
  }

}

function toTaxJurisdictions(value: unknown): TaxJurisdiction[] {
  if (!Array.isArray(value) || !value.every(isTaxJurisdiction)) {
    throw new Error('The tax jurisdictions response contains invalid records.');
  }
  return value;
}

function isTaxJurisdiction(value: unknown): value is TaxJurisdiction {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return typeof row['id'] === 'number' && Number.isSafeInteger(row['id']) &&
    typeof row['code'] === 'string' && typeof row['countryCode'] === 'string';
}
