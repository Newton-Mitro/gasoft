import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import {
  AddExistingTenantUserPayload,
  CreateTenantUserPayload,
  TenantUser,
  UpdateTenantUserPayload,
} from './user.model';

const ENDPOINT = '/api/v1/tenant/users';

@Injectable({ providedIn: 'root' })
export class TenantUserService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<TenantUser[]> {
    return this.api.get<unknown>(ENDPOINT, false).pipe(map(toTenantUsers));
  }

  getById(id: number): Observable<TenantUser> {
    return this.api.get<unknown>(`${ENDPOINT}/${id}`, false).pipe(map(toTenantUser));
  }

  create(payload: CreateTenantUserPayload): Observable<TenantUser> {
    return this.api.post<unknown>(ENDPOINT, payload, false).pipe(map(toTenantUser));
  }

  addExisting(payload: AddExistingTenantUserPayload): Observable<TenantUser> {
    return this.api.post<unknown>(`${ENDPOINT}/existing`, payload, false).pipe(map(toTenantUser));
  }

  update(id: number, payload: UpdateTenantUserPayload): Observable<TenantUser> {
    return this.api.put<unknown>(`${ENDPOINT}/${id}`, payload, false).pipe(map(toTenantUser));
  }
}

function toTenantUsers(result: unknown): TenantUser[] {
  if (!Array.isArray(result)) throw new Error('The team members response has an unsupported format.');
  if (!result.every(isTenantUser)) throw new Error('The team members response contains an invalid record.');
  return result;
}

function toTenantUser(result: unknown): TenantUser {
  if (!isTenantUser(result)) throw new Error('The team member response contains an invalid record.');
  return result;
}

function isTenantUser(value: unknown): value is TenantUser {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return typeof row['id'] === 'number' && Number.isSafeInteger(row['id']) &&
    typeof row['email'] === 'string' &&
    typeof row['displayName'] === 'string' &&
    typeof row['role'] === 'string' &&
    typeof row['isActive'] === 'boolean';
}
