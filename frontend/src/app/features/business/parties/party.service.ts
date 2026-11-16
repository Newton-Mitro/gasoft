import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { Party, PartyPayload } from './party.model';

const ENDPOINT = '/api/v1/parties';

@Injectable({ providedIn: 'root' })
export class PartyService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<Party[]> {
    return this.api.get<unknown>(ENDPOINT, false).pipe(map(toParties));
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<Party>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>(`${ENDPOINT}/page`, false, params)
      .pipe(map(result => parsePagedResult(result, isParty, 'customers and suppliers')));
  }

  getById(id: number): Observable<Party> {
    return this.api.get<Party>(`${ENDPOINT}/${id}`, false);
  }

  create(payload: PartyPayload): Observable<Party> {
    return this.api.post<Party>(ENDPOINT, payload, false);
  }

  update(id: number, payload: PartyPayload): Observable<Party> {
    return this.api.put<Party>(`${ENDPOINT}/${id}`, payload, false);
  }
}

function toParties(result: unknown): Party[] {
  const rows = Array.isArray(result)
    ? result
    : result && typeof result === 'object' && Array.isArray((result as Record<string, unknown>)['items'])
      ? (result as Record<string, unknown>)['items'] as unknown[]
      : null;
  if (!rows) throw new Error('The customers and suppliers response has an unsupported format.');
  if (!rows.every(isParty)) throw new Error('The customers and suppliers response contains an invalid record.');
  return rows;
}

function isParty(value: unknown): value is Party {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    typeof row['partyNo'] === 'string' &&
    typeof row['displayName'] === 'string' &&
    typeof row['isCustomer'] === 'boolean' &&
    typeof row['isSupplier'] === 'boolean' &&
    isOptionalString(row['email']) &&
    isOptionalString(row['phone']) &&
    isOptionalString(row['defaultCurrencyCode']);
}

function isOptionalString(value: unknown): value is string | null {
  return value === null || typeof value === 'string';
}
