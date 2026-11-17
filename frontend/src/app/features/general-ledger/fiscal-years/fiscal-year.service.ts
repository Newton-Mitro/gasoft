import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import {
  FiscalYear,
  FiscalYearCloseChecklist,
  FiscalYearPayload,
  FiscalYearStatusChange,
} from './fiscal-year.model';

@Injectable({ providedIn: 'root' })
export class FiscalYearService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<FiscalYear[]> {
    return this.api.get<unknown>('fiscal-years', true).pipe(map(toFiscalYears));
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<FiscalYear>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>('fiscal-years/page', true, params)
      .pipe(map(result => parsePagedResult(result, isFiscalYear, 'fiscal years')));
  }

  getById(id: number): Observable<FiscalYear> {
    return this.api.get<FiscalYear>(`fiscal-years/${id}`, true);
  }

  create(payload: FiscalYearPayload): Observable<FiscalYear> {
    return this.api.post<FiscalYear>('fiscal-years', payload, true);
  }

  update(id: number, payload: FiscalYearPayload): Observable<FiscalYear> {
    return this.api.put<FiscalYear>(`fiscal-years/${id}`, payload, true);
  }

  getCloseChecklist(id: number): Observable<FiscalYearCloseChecklist> {
    return this.api.get<unknown>(`fiscal-years/${id}/close-checklist`, true)
      .pipe(map(toCloseChecklist));
  }

  close(id: number, reason: string): Observable<FiscalYear> {
    return this.api.post<FiscalYear>(`fiscal-years/${id}/close`, { reason }, true);
  }

  prepareClosingJournal(
    id: number,
    retainedEarningsAccountId: number,
  ): Observable<{ id: number; status: string }> {
    return this.api.post<{ id: number; status: string }>(
      `fiscal-years/${id}/closing-journal`,
      { retainedEarningsAccountId },
      true,
    );
  }

  prepareOpeningBalances(id: number): Observable<{ id: number; status: string }> {
    return this.api.post<{ id: number; status: string }>(
      `fiscal-years/${id}/opening-balances`,
      {},
      true,
    );
  }

  postClosingJournal(journalEntryId: number): Observable<{ id: number; status: string }> {
    return this.api.post<{ id: number; status: string }>(
      `journal-entries/${journalEntryId}/post`,
      {},
      true,
    );
  }

  reopen(id: number, reason: string): Observable<FiscalYear> {
    return this.api.post<FiscalYear>(`fiscal-years/${id}/reopen`, { reason }, true);
  }

  getStatusHistory(id: number): Observable<FiscalYearStatusChange[]> {
    return this.api.get<unknown>(`fiscal-years/${id}/status-history`, true)
      .pipe(map(toStatusHistory));
  }
}

function toFiscalYears(result: unknown): FiscalYear[] {
  const rows = Array.isArray(result)
    ? result
    : result && typeof result === 'object' && Array.isArray((result as { items?: unknown }).items)
      ? (result as { items: unknown[] }).items
      : null;
  if (!rows) throw new Error('The fiscal years response has an unsupported format.');
  if (!rows.every(isFiscalYear)) throw new Error('The fiscal years response contains an invalid record.');
  return rows;
}

function isFiscalYear(value: unknown): value is FiscalYear {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    typeof row['startsOn'] === 'string' &&
    typeof row['endsOn'] === 'string' &&
    typeof row['status'] === 'string';
}

function toCloseChecklist(value: unknown): FiscalYearCloseChecklist {
  if (!value || typeof value !== 'object') throw new Error('The fiscal-year close checklist response contains invalid data.');
  const row = value as Record<string, unknown>;
  if (!Number.isSafeInteger(row['fiscalYearId']) ||
      !Number.isSafeInteger(row['accountingPeriodCount']) ||
      !Number.isSafeInteger(row['openAccountingPeriodCount']) ||
      !Number.isSafeInteger(row['draftJournalEntryCount']) ||
      typeof row['closingJournalRequired'] !== 'boolean' ||
      !(row['closingJournalEntryId'] === null || Number.isSafeInteger(row['closingJournalEntryId'])) ||
      !(row['closingJournalStatus'] === null || typeof row['closingJournalStatus'] === 'string') ||
      !Array.isArray(row['closingJournalLines']) || !row['closingJournalLines'].every(isClosingLine) ||
      typeof row['canClose'] !== 'boolean')
    throw new Error('The fiscal-year close checklist response contains invalid data.');
  return value as FiscalYearCloseChecklist;
}

function isClosingLine(value: unknown): boolean {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return typeof row['accountCode'] === 'string' &&
    typeof row['accountName'] === 'string' &&
    typeof row['debit'] === 'number' && Number.isFinite(row['debit']) &&
    typeof row['credit'] === 'number' && Number.isFinite(row['credit']);
}

function toStatusHistory(value: unknown): FiscalYearStatusChange[] {
  if (!Array.isArray(value) || !value.every(isStatusChange))
    throw new Error('The fiscal-year status history response contains invalid data.');
  return value;
}

function isStatusChange(value: unknown): value is FiscalYearStatusChange {
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
