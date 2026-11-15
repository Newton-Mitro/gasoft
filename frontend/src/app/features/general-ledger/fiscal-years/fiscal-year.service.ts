import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { FiscalYear, FiscalYearPayload } from './fiscal-year.model';

@Injectable({ providedIn: 'root' })
export class FiscalYearService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<FiscalYear[]> {
    return this.api.get<unknown>('fiscal-years', true).pipe(map(toFiscalYears));
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
