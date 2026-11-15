import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { TaxJurisdiction, TaxJurisdictionPayload } from './tax-jurisdiction.model';

const ENDPOINT = '/api/v1/tax-jurisdictions';

@Injectable({ providedIn: 'root' })
export class TaxJurisdictionService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<TaxJurisdiction[]> {
    return this.api.get<unknown>(ENDPOINT, false).pipe(map(toTaxJurisdictions));
  }

  getById(id: number): Observable<TaxJurisdiction> {
    return this.api.get<unknown>(`${ENDPOINT}/${id}`, false).pipe(map(toTaxJurisdiction));
  }

  create(payload: TaxJurisdictionPayload): Observable<TaxJurisdiction> {
    return this.api.post<unknown>(ENDPOINT, payload, false).pipe(map(toTaxJurisdiction));
  }

  update(id: number, payload: TaxJurisdictionPayload): Observable<TaxJurisdiction> {
    return this.api.put<unknown>(`${ENDPOINT}/${id}`, payload, false).pipe(map(toTaxJurisdiction));
  }

  delete(id: number): Observable<void> {
    return this.api.delete(`${ENDPOINT}/${id}`, false);
  }
}

function toTaxJurisdictions(value: unknown): TaxJurisdiction[] {
  if (!Array.isArray(value) || !value.every(isTaxJurisdiction)) {
    throw new Error('The tax jurisdictions response contains invalid records.');
  }
  return value;
}

function toTaxJurisdiction(value: unknown): TaxJurisdiction {
  if (!isTaxJurisdiction(value)) {
    throw new Error('The tax jurisdiction response contains an invalid record.');
  }
  return value;
}

function isTaxJurisdiction(value: unknown): value is TaxJurisdiction {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return typeof row['id'] === 'number' && Number.isSafeInteger(row['id']) &&
    typeof row['code'] === 'string' && typeof row['countryCode'] === 'string';
}
