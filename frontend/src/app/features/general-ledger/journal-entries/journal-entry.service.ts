import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { JournalEntry, JournalEntryPayload } from './journal-entry.model';

const ENDPOINT = 'journal-entries';

@Injectable({ providedIn: 'root' })
export class JournalEntryService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<JournalEntry[]> {
    return this.api.get<unknown>(ENDPOINT, true).pipe(map(toJournalEntries));
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<JournalEntry>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>(`${ENDPOINT}/page`, true, params)
      .pipe(map(result => parsePagedResult(result, isJournalEntry, 'journal entries')));
  }

  getById(id: number): Observable<JournalEntry> {
    return this.api.get<unknown>(`${ENDPOINT}/${id}`, true).pipe(map(toJournalEntry));
  }

  create(payload: JournalEntryPayload): Observable<JournalEntry> {
    return this.api.post<unknown>(ENDPOINT, payload, true).pipe(map(toJournalEntry));
  }

  update(id: number, payload: JournalEntryPayload): Observable<JournalEntry> {
    return this.api.put<unknown>(`${ENDPOINT}/${id}`, payload, true).pipe(map(toJournalEntry));
  }
}

function toJournalEntries(result: unknown): JournalEntry[] {
  const rows = Array.isArray(result)
    ? result
    : result && typeof result === 'object' && Array.isArray((result as Record<string, unknown>)['items'])
      ? (result as Record<string, unknown>)['items'] as unknown[]
      : null;
  if (!rows) throw new Error('The journal entries response has an unsupported format.');
  if (!rows.every(isJournalEntry)) throw new Error('The journal entries response contains an invalid record.');
  return rows;
}

function toJournalEntry(result: unknown): JournalEntry {
  if (!isJournalEntry(result)) throw new Error('The journal entry response contains an invalid record.');
  return result;
}

function isJournalEntry(value: unknown): value is JournalEntry {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return isId(row['id']) &&
    isId(row['companyId']) &&
    isId(row['journalId']) &&
    isId(row['accountingPeriodId']) &&
    typeof row['entryDate'] === 'string' &&
    typeof row['description'] === 'string' &&
    typeof row['status'] === 'string' &&
    isNumericValue(row['totalDebit']) &&
    isNumericValue(row['totalCredit']) &&
    Array.isArray(row['lines']) &&
    row['lines'].every(isJournalEntryLine);
}

function isJournalEntryLine(value: unknown): value is JournalEntry['lines'][number] {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return isId(row['id']) &&
    isId(row['accountId']) &&
    typeof row['transactionCurrencyCode'] === 'string' &&
    isNumericValue(row['debit']) &&
    isNumericValue(row['credit']) &&
    isNumericValue(row['functionalDebit']) &&
    isNumericValue(row['functionalCredit']) &&
    isNumericValue(row['exchangeRate']) &&
    (row['description'] === null || typeof row['description'] === 'string');
}

function isId(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

function isNumericValue(value: unknown): value is number | string {
  return (typeof value === 'number' && Number.isFinite(value)) ||
    (typeof value === 'string' && value.trim() !== '' && Number.isFinite(Number(value)));
}
