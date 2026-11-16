import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { TaxCode, TaxCodePayload, TaxRate, TaxRatePayload } from './tax-code.model';

const ENDPOINT = 'tax-codes';

@Injectable({ providedIn: 'root' })
export class TaxCodeService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<TaxCode[]> {
    return this.api.get<unknown>(ENDPOINT).pipe(map(toTaxCodes));
  }

  create(payload: TaxCodePayload): Observable<TaxCode> {
    return this.api.post<unknown>(ENDPOINT, payload).pipe(map(toTaxCode));
  }

  update(id: number, payload: TaxCodePayload): Observable<TaxCode> {
    return this.api.put<unknown>(`${ENDPOINT}/${id}`, payload).pipe(map(toTaxCode));
  }

  delete(id: number): Observable<void> {
    return this.api.delete(`${ENDPOINT}/${id}`);
  }

  createRate(taxCodeId: number, payload: TaxRatePayload): Observable<TaxRate> {
    return this.api.post<unknown>(`${ENDPOINT}/${taxCodeId}/rates`, payload).pipe(map(toTaxRate));
  }

  updateRate(taxCodeId: number, rateId: number, payload: TaxRatePayload): Observable<TaxRate> {
    return this.api.put<unknown>(`${ENDPOINT}/${taxCodeId}/rates/${rateId}`, payload).pipe(map(toTaxRate));
  }

  deleteRate(taxCodeId: number, rateId: number): Observable<void> {
    return this.api.delete(`${ENDPOINT}/${taxCodeId}/rates/${rateId}`);
  }
}

function toTaxCodes(value: unknown): TaxCode[] {
  if (!Array.isArray(value) || !value.every(isTaxCode)) {
    throw new Error('The tax codes response contains invalid records.');
  }
  return value;
}

function toTaxCode(value: unknown): TaxCode {
  if (!isTaxCode(value)) throw new Error('The tax code response contains an invalid record.');
  return value;
}

function toTaxRate(value: unknown): TaxRate {
  if (!isTaxRate(value)) throw new Error('The tax rate response contains an invalid record.');
  return value;
}

function isTaxCode(value: unknown): value is TaxCode {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return isPositiveInteger(row['id']) &&
    isPositiveInteger(row['jurisdictionId']) &&
    isPositiveInteger(row['categoryId']) &&
    typeof row['jurisdictionCode'] === 'string' &&
    typeof row['countryCode'] === 'string' &&
    typeof row['categoryCode'] === 'string' &&
    typeof row['categoryName'] === 'string' &&
    typeof row['code'] === 'string' &&
    typeof row['taxType'] === 'string' &&
    Array.isArray(row['rates']) &&
    row['rates'].every(isTaxRate);
}

function isTaxRate(value: unknown): value is TaxRate {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return isPositiveInteger(row['id']) &&
    isPositiveInteger(row['taxCodeId']) &&
    typeof row['ratePercent'] === 'number' &&
    typeof row['validFrom'] === 'string' &&
    (row['validTo'] === null || typeof row['validTo'] === 'string');
}

function isPositiveInteger(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}
