import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService } from '../../core/api.service';
import {
  ConsolidationCompany,
  ConsolidationElimination,
  ConsolidationGroup,
  ConsolidationGroupPayload,
  ConsolidationRateInput,
  ConsolidationRun,
  ConsolidationRunSummary,
} from './consolidation.model';

const ENDPOINT = '/api/v1/tenant/consolidation-groups';

@Injectable({ providedIn: 'root' })
export class ConsolidationService {
  constructor(private readonly api: ApiService) {}

  getGroups(): Observable<ConsolidationGroup[]> {
    return this.api.get<unknown>(ENDPOINT, false).pipe(map(value => {
      if (!Array.isArray(value) || !value.every(isGroup)) {
        throw new Error('The consolidation groups response contains invalid records.');
      }
      return value;
    }));
  }

  getCompanies(): Observable<ConsolidationCompany[]> {
    return this.api.get<unknown>(`${ENDPOINT}/companies`, false).pipe(map(value => {
      if (!Array.isArray(value) || !value.every(isCompany)) {
        throw new Error('The consolidation companies response contains invalid records.');
      }
      return value;
    }));
  }

  createGroup(payload: ConsolidationGroupPayload): Observable<ConsolidationGroup> {
    return this.api.post<unknown>(ENDPOINT, payload, false).pipe(map(toGroup));
  }

  updateGroup(groupId: number, payload: ConsolidationGroupPayload): Observable<ConsolidationGroup> {
    return this.api.put<unknown>(`${ENDPOINT}/${groupId}`, payload, false).pipe(map(toGroup));
  }

  getRuns(groupId: number): Observable<ConsolidationRunSummary[]> {
    return this.api.get<unknown>(`${ENDPOINT}/${groupId}/runs`, false).pipe(map(value => {
      if (!Array.isArray(value) || !value.every(isRunSummary)) {
        throw new Error('The consolidation runs response contains invalid records.');
      }
      return value;
    }));
  }

  createRun(
    groupId: number,
    periodStart: string,
    periodEnd: string,
    translationRates: ConsolidationRateInput[],
  ): Observable<ConsolidationRun> {
    return this.api.post<unknown>(`${ENDPOINT}/${groupId}/runs`, {
      periodStart,
      periodEnd,
      translationRates,
    }, false).pipe(map(toRun));
  }

  getRun(groupId: number, runId: number): Observable<ConsolidationRun> {
    return this.api.get<unknown>(`${ENDPOINT}/${groupId}/runs/${runId}`, false).pipe(map(toRun));
  }

  saveEliminations(groupId: number, runId: number, lines: ConsolidationElimination[]): Observable<void> {
    return this.api.put<void>(`${ENDPOINT}/${groupId}/runs/${runId}/eliminations`, { lines }, false);
  }

  finalizeRun(groupId: number, runId: number): Observable<unknown> {
    return this.api.post(`${ENDPOINT}/${groupId}/runs/${runId}/finalize`, {}, false);
  }
}

function toGroup(value: unknown): ConsolidationGroup {
  if (!isGroup(value)) throw new Error('The consolidation group response contains an invalid record.');
  return value;
}

function toRun(value: unknown): ConsolidationRun {
  if (!isRun(value)) throw new Error('The consolidation run response contains an invalid record.');
  return value;
}

function isGroup(value: unknown): value is ConsolidationGroup {
  if (!isRecord(value)) return false;
  return isPositiveInteger(value['id']) &&
    typeof value['code'] === 'string' &&
    typeof value['name'] === 'string' &&
    isCurrency(value['reportingCurrencyCode']) &&
    Array.isArray(value['companies']) &&
    value['companies'].every(isCompany);
}

function isCompany(value: unknown): value is ConsolidationCompany {
  if (!isRecord(value)) return false;
  return isPositiveInteger(value['id']) &&
    typeof value['code'] === 'string' &&
    typeof value['legalName'] === 'string' &&
    isCurrency(value['functionalCurrencyCode']);
}

function isRunSummary(value: unknown): value is ConsolidationRunSummary {
  if (!isRecord(value)) return false;
  return isPositiveInteger(value['id']) &&
    isDate(value['periodEnd']) &&
    typeof value['status'] === 'string';
}

function isRun(value: unknown): value is ConsolidationRun {
  if (!isRecord(value)) return false;
  return isPositiveInteger(value['id']) &&
    isDate(value['periodStart']) &&
    isDate(value['periodEnd']) &&
    typeof value['status'] === 'string' &&
    isCurrency(value['reportingCurrencyCode']) &&
    Array.isArray(value['accounts']) &&
    value['accounts'].every(isAccount) &&
    Array.isArray(value['translationRates']) &&
    value['translationRates'].every(isTranslationRate) &&
    Array.isArray(value['eliminations']) &&
    value['eliminations'].every(isElimination) &&
    Array.isArray(value['translationAdjustments']) &&
    value['translationAdjustments'].every(isElimination);
}

function isTranslationRate(value: unknown): boolean {
  return isRecord(value) &&
    isPositiveInteger(value['companyId']) &&
    isCurrency(value['sourceCurrencyCode']) &&
    isCurrency(value['targetCurrencyCode']) &&
    typeof value['closingRate'] === 'number' && Number.isFinite(value['closingRate']) && value['closingRate'] > 0 &&
    typeof value['averageRate'] === 'number' && Number.isFinite(value['averageRate']) && value['averageRate'] > 0;
}

function isElimination(value: unknown): boolean {
  return isRecord(value) &&
    typeof value['accountCode'] === 'string' &&
    typeof value['accountName'] === 'string' &&
    typeof value['debit'] === 'number' && Number.isFinite(value['debit']) &&
    typeof value['credit'] === 'number' && Number.isFinite(value['credit']) &&
    (value['description'] === null || typeof value['description'] === 'string');
}

function isAccount(value: unknown): boolean {
  if (!isRecord(value)) return false;
  return typeof value['accountCode'] === 'string' &&
    typeof value['accountName'] === 'string' &&
    typeof value['debit'] === 'number' && Number.isFinite(value['debit']) &&
    typeof value['credit'] === 'number' && Number.isFinite(value['credit']);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value && typeof value === 'object' && !Array.isArray(value));
}

function isPositiveInteger(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

function isCurrency(value: unknown): value is string {
  return typeof value === 'string' && /^[A-Z]{3}$/.test(value);
}

function isDate(value: unknown): value is string {
  return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value);
}
