import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { PartyAddress, PartyAddressPayload } from './party-address.model';

@Injectable({ providedIn: 'root' })
export class PartyAddressService {
  constructor(private readonly api: ApiService) {}

  getAll(partyId: number): Observable<PartyAddress[]> {
    return this.api.get<unknown>(endpointForParty(partyId), false).pipe(map(toAddresses));
  }

  getPage(partyId: number, page: number, pageSize: number): Observable<PagedResult<PartyAddress>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>(`${endpointForParty(partyId)}/page`, false, params)
      .pipe(map(result => parsePagedResult(result, isPartyAddress, 'party addresses')));
  }

  getById(partyId: number, addressId: number): Observable<PartyAddress> {
    return this.api.get<unknown>(endpointForAddress(partyId, addressId), false).pipe(map(toAddress));
  }

  create(partyId: number, payload: PartyAddressPayload): Observable<PartyAddress> {
    return this.api.post<unknown>(endpointForParty(partyId), payload, false).pipe(map(toAddress));
  }

  update(partyId: number, addressId: number, payload: PartyAddressPayload): Observable<PartyAddress> {
    return this.api.put<unknown>(endpointForAddress(partyId, addressId), payload, false).pipe(map(toAddress));
  }
}

function endpointForAddress(partyId: number, addressId: number): string {
  if (!Number.isSafeInteger(addressId) || addressId < 1) throw new Error('A valid address ID is required.');
  return `${endpointForParty(partyId)}/${addressId}`;
}

function endpointForParty(partyId: number): string {
  if (!Number.isSafeInteger(partyId) || partyId < 1) throw new Error('A valid party ID is required.');
  return `/api/v1/parties/${partyId}/addresses`;
}

function toAddress(result: unknown): PartyAddress {
  if (!isPartyAddress(result)) throw new Error('The party address response contains an invalid record.');
  return result;
}

function toAddresses(result: unknown): PartyAddress[] {
  const rows = Array.isArray(result)
    ? result
    : result && typeof result === 'object' && Array.isArray((result as Record<string, unknown>)['items'])
      ? (result as Record<string, unknown>)['items'] as unknown[]
      : null;
  if (!rows) throw new Error('The party addresses response has an unsupported format.');
  if (!rows.every(isPartyAddress)) throw new Error('The party addresses response contains an invalid record.');
  return rows;
}

function isPartyAddress(value: unknown): value is PartyAddress {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  const id = row['id'];
  return typeof id === 'number' && Number.isSafeInteger(id) && id > 0 &&
    (row['addressType'] === 'Billing' || row['addressType'] === 'Shipping' || row['addressType'] === 'Other') &&
    typeof row['line1'] === 'string' &&
    isOptionalString(row['line2']) &&
    typeof row['city'] === 'string' &&
    isOptionalString(row['region']) &&
    isOptionalString(row['postalCode']) &&
    typeof row['countryCode'] === 'string' &&
    typeof row['isDefault'] === 'boolean';
}

function isOptionalString(value: unknown): value is string | null {
  return value === null || typeof value === 'string';
}
