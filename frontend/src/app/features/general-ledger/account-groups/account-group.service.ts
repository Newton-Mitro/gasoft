import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { AccountGroup, AccountGroupPage, AccountGroupPayload, AccountType } from './account-group.model';

@Injectable({ providedIn: 'root' })
export class AccountGroupService {
  constructor(private readonly api: ApiService) {}

  getPage(search: string, page: number, pageSize: number): Observable<AccountGroupPage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search.trim()) params = params.set('search', search.trim());
    return this.api.get<unknown>('account-groups/page', true, params).pipe(map(toAccountGroupPage));
  }

  getById(id: number): Observable<AccountGroup> {
    return this.api.get<AccountGroup>(`account-groups/${id}`, true);
  }

  create(payload: AccountGroupPayload): Observable<AccountGroup> {
    return this.api.post<AccountGroup>('account-groups', payload, true);
  }

  update(id: number, payload: AccountGroupPayload): Observable<AccountGroup> {
    return this.api.put<AccountGroup>(`account-groups/${id}`, payload, true);
  }
}

function toAccountGroupPage(result: unknown): AccountGroupPage {
  if (!result || typeof result !== 'object') {
    throw new Error('The account groups response has an unsupported format.');
  }
  const page = result as Record<string, unknown>;
  const items = page['items'];
  if (!Array.isArray(items) || !items.every(isAccountGroup) ||
      !Number.isSafeInteger(page['page']) || !Number.isSafeInteger(page['pageSize']) ||
      !Number.isSafeInteger(page['totalCount']) || !Number.isSafeInteger(page['totalPages'])) {
    throw new Error('The account groups response contains invalid page data.');
  }
  return {
    items,
    page: Number(page['page']),
    pageSize: Number(page['pageSize']),
    totalCount: Number(page['totalCount']),
    totalPages: Number(page['totalPages']),
  };
}

function isAccountGroup(value: unknown): value is AccountGroup {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    Number.isSafeInteger(row['companyId']) &&
    typeof row['code'] === 'string' &&
    typeof row['name'] === 'string' &&
    isAccountType(row['accountType']) &&
    typeof row['normalBalance'] === 'string';
}

function isAccountType(value: unknown): value is AccountType {
  return value === 'Asset' || value === 'Liability' || value === 'Equity' ||
    value === 'Revenue' || value === 'Expense';
}
