import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import {
  AccountingPeriod,
  AccountingPeriodPayload,
  AccountingPeriodCloseChecklist,
  AccountingPeriodStatusChange,
} from './period.model';

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

  close(id: number, reason: string): Observable<AccountingPeriod> {
    return this.api.post<AccountingPeriod>(`accounting-periods/${id}/close`, { reason }, true);
  }

  getCloseChecklist(id: number): Observable<AccountingPeriodCloseChecklist> {
    return this.api.get<unknown>(`accounting-periods/${id}/close-checklist`, true)
      .pipe(map(toCloseChecklist));
  }

  reopen(id: number, reason: string): Observable<AccountingPeriod> {
    return this.api.post<AccountingPeriod>(`accounting-periods/${id}/reopen`, { reason }, true);
  }

  getStatusHistory(id: number): Observable<AccountingPeriodStatusChange[]> {
    return this.api.get<unknown>(`accounting-periods/${id}/status-history`, true)
      .pipe(map(toStatusHistory));
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

function toStatusHistory(value: unknown): AccountingPeriodStatusChange[] {
  if (!Array.isArray(value) || !value.every(isStatusChange))
    throw new Error('The accounting period history response contains invalid data.');
  return value;
}

function isStatusChange(value: unknown): value is AccountingPeriodStatusChange {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    typeof row['fromStatus'] === 'string' &&
    typeof row['toStatus'] === 'string' &&
    typeof row['reason'] === 'string' &&
    typeof row['changedAt'] === 'string' &&
    Number.isSafeInteger(row['changedByUserId']) &&
    typeof row['changedByDisplayName'] === 'string';
}

function toCloseChecklist(value: unknown): AccountingPeriodCloseChecklist {
  if (!value || typeof value !== 'object') throw new Error('The period close checklist response contains invalid data.');
  const row = value as Record<string, unknown>;
  if (!Number.isSafeInteger(row['periodId']) ||
      !Number.isSafeInteger(row['draftJournalEntryCount']) ||
      !Number.isSafeInteger(row['unreconciledBankTransactionCount']) ||
      typeof row['canClose'] !== 'boolean')
    throw new Error('The period close checklist response contains invalid data.');
  return value as AccountingPeriodCloseChecklist;
}
