import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { TaxRegistration, TaxRegistrationMutation, TaxRegistrationPayload } from './tax-registration.model';

const ENDPOINT = 'tax-registrations';

@Injectable({ providedIn: 'root' })
export class TaxRegistrationService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<TaxRegistration[]> {
    return this.api.get<unknown>(ENDPOINT, true).pipe(map(toTaxRegistrations));
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<TaxRegistration>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>(`${ENDPOINT}/page`, true, params)
      .pipe(map(result => parsePagedResult(result, isTaxRegistration, 'tax registrations')));
  }

  getById(id: number): Observable<TaxRegistration> {
    return this.api.get<unknown>(`${ENDPOINT}/${id}`, true).pipe(map(toTaxRegistration));
  }

  create(payload: TaxRegistrationPayload): Observable<TaxRegistrationMutation> {
    return this.api.post<unknown>(ENDPOINT, payload, true).pipe(map(toTaxRegistrationMutation));
  }

  update(id: number, payload: TaxRegistrationPayload): Observable<TaxRegistrationMutation> {
    return this.api.put<unknown>(`${ENDPOINT}/${id}`, payload, true).pipe(map(toTaxRegistrationMutation));
  }
}

function toTaxRegistrations(result: unknown): TaxRegistration[] {
  const rows = Array.isArray(result)
    ? result
    : result && typeof result === 'object' && Array.isArray((result as Record<string, unknown>)['items'])
      ? (result as Record<string, unknown>)['items'] as unknown[]
      : null;
  if (!rows) throw new Error('The tax registrations response has an unsupported format.');
  if (!rows.every(isTaxRegistration)) throw new Error('The tax registrations response contains an invalid record.');
  return rows;
}

function toTaxRegistration(result: unknown): TaxRegistration {
  if (!isTaxRegistration(result)) throw new Error('The tax registration response contains an invalid record.');
  return result;
}

function toTaxRegistrationMutation(result: unknown): TaxRegistrationMutation {
  if (!isTaxRegistrationMutation(result)) {
    throw new Error('The tax registration response contains an invalid record.');
  }
  return result;
}

function isTaxRegistrationMutation(value: unknown): value is TaxRegistrationMutation {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return typeof row['id'] === 'number' && Number.isSafeInteger(row['id']) &&
    typeof row['companyId'] === 'number' && Number.isSafeInteger(row['companyId']) &&
    typeof row['jurisdictionId'] === 'number' && Number.isSafeInteger(row['jurisdictionId']) &&
    typeof row['registrationNumber'] === 'string';
}

function isTaxRegistration(value: unknown): value is TaxRegistration {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    Number.isSafeInteger(row['companyId']) &&
    Number.isSafeInteger(row['jurisdictionId']) &&
    typeof row['code'] === 'string' &&
    typeof row['countryCode'] === 'string' &&
    typeof row['registrationNumber'] === 'string';
}
