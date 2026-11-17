import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { Account, AccountPage, AccountPayload } from './account.model';

@Injectable({ providedIn: 'root' })
export class AccountService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<Account[]> {
    return this.api.get<unknown>('accounts', true).pipe(map(toAccounts));
  }

  getPage(search: string, page: number, pageSize: number): Observable<AccountPage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search.trim()) params = params.set('search', search.trim());
    return this.api.get<unknown>('accounts/page', true, params).pipe(map(toAccountPage));
  }

  getById(id: number): Observable<Account> {
    return this.api.get<Account>(`accounts/${id}`, true);
  }

  create(payload: AccountPayload): Observable<Account> {
    return this.api.post<Account>('accounts', payload, true);
  }

  update(id: number, payload: AccountPayload): Observable<Account> {
    return this.api.put<Account>(`accounts/${id}`, payload, true);
  }
}

function toAccounts(result: unknown): Account[] {
  if (!Array.isArray(result) || !result.every(isAccount)) {
    throw new Error('The accounts response contains invalid account data.');
  }
  return result;
}

function toAccountPage(result: unknown): AccountPage {
  if (!result || typeof result !== 'object') {
    throw new Error('The accounts response has an unsupported format.');
  }
  const page = result as Record<string, unknown>;
  const items = page['items'];
  if (!Array.isArray(items) || !items.every(isAccount) ||
      !Number.isSafeInteger(page['page']) || !Number.isSafeInteger(page['pageSize']) ||
      !Number.isSafeInteger(page['totalCount']) || !Number.isSafeInteger(page['totalPages']) ||
      !Number.isSafeInteger(page['totalAccounts'])) {
    throw new Error('The accounts response contains invalid page data.');
  }
  return {
    items,
    page: Number(page['page']),
    pageSize: Number(page['pageSize']),
    totalCount: Number(page['totalCount']),
    totalPages: Number(page['totalPages']),
    totalAccounts: Number(page['totalAccounts']),
  };
}

function isAccount(value: unknown): value is Account {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    Number.isSafeInteger(row['companyId']) &&
    Number.isSafeInteger(row['accountGroupId']) &&
    (row['parentId'] === null || Number.isSafeInteger(row['parentId'])) &&
    typeof row['code'] === 'string' &&
    typeof row['name'] === 'string' &&
    typeof row['allowPosting'] === 'boolean' &&
    typeof row['isActive'] === 'boolean';
}
