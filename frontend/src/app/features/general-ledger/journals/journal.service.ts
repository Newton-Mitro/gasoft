import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { Journal, JournalPayload } from './journal.model';

@Injectable({ providedIn: 'root' })
export class JournalService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<Journal[]> {
    return this.api.get<unknown>('journals', true).pipe(map(toJournals));
  }

  getById(id: number): Observable<Journal> {
    return this.api.get<Journal>(`journals/${id}`, true);
  }

  create(payload: JournalPayload): Observable<Journal> {
    return this.api.post<Journal>('journals', payload, true);
  }

  update(id: number, payload: JournalPayload): Observable<Journal> {
    return this.api.put<Journal>(`journals/${id}`, payload, true);
  }
}

function toJournals(result: unknown): Journal[] {
  const rows = Array.isArray(result)
    ? result
    : result && typeof result === 'object' && Array.isArray((result as { items?: unknown }).items)
      ? (result as { items: unknown[] }).items
      : null;
  if (!rows) throw new Error('The journals response has an unsupported format.');
  if (!rows.every(isJournal)) throw new Error('The journals response contains an invalid record.');
  return rows;
}

function isJournal(value: unknown): value is Journal {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) &&
    typeof row['code'] === 'string' &&
    typeof row['name'] === 'string' &&
    ['General', 'Sales', 'Purchase', 'Bank'].includes(String(row['journalType'])) &&
    typeof row['isActive'] === 'boolean';
}
