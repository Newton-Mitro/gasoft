import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { AccountingPeriod, AccountingPeriodPayload } from './period.model';

@Injectable({ providedIn: 'root' })
export class AccountingPeriodService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<AccountingPeriod[]> {
    return this.api.get<unknown>('accounting-periods', true).pipe(map(toAccountingPeriods));
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<AccountingPeriod>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>('accounting-periods/page', true, params)
      .pipe(map(result => parsePagedResult(result, isAccountingPeriod, 'accounting periods')));
  }

  getById(id: number): Observable<AccountingPeriod> {
    return this.api.get<AccountingPeriod>(`accounting-periods/${id}`, true);
  }

  create(payload: AccountingPeriodPayload): Observable<AccountingPeriod> {
    return this.api.post<AccountingPeriod>('accounting-periods', payload, true);
  }

  update(id: number, payload: AccountingPeriodPayload): Observable<AccountingPeriod> {
    return this.api.put<AccountingPeriod>(`accounting-periods/${id}`, payload, true);
  }
}

function toAccountingPeriods(result: unknown): AccountingPeriod[] {
  const rows = Array.isArray(result)
    ? result
    : result && typeof result === 'object' && Array.isArray((result as { items?: unknown }).items)
      ? (result as { items: unknown[] }).items
      : null;
  if (!rows) throw new Error('The accounting periods response has an unsupported format.');
  if (!rows.every(isAccountingPeriod)) throw new Error('The accounting periods response contains an invalid record.');
  return rows;
}

function isAccountingPeriod(value: unknown): value is AccountingPeriod {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    Number.isSafeInteger(row['fiscalYearId']) &&
    typeof row['startsOn'] === 'string' &&
    typeof row['endsOn'] === 'string' &&
    typeof row['status'] === 'string';
}
