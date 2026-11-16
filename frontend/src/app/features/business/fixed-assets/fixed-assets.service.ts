import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { parsePagedResult } from '../../../shared/utils/paged-result';
import { FixedAsset, FixedAssetCreatePayload, FixedAssetSetup, PagedFixedAssetSetup } from './fixed-assets.model';

@Injectable({ providedIn: 'root' })
export class FixedAssetsService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<FixedAssetSetup> {
    return this.api.get<unknown>('fixed-assets', true).pipe(map(toSetup));
  }

  getPage(page: number, pageSize: number): Observable<PagedFixedAssetSetup> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.api.get<unknown>('fixed-assets/page', true, params).pipe(map(toPagedSetup));
  }

  create(payload: FixedAssetCreatePayload): Observable<unknown> {
    return this.api.post('fixed-assets', payload, true);
  }

  depreciate(assetId: number, throughAccountingPeriodId: number, journalId: number): Observable<unknown> {
    return this.api.post(`fixed-assets/${assetId}/depreciate`, {
      throughAccountingPeriodId,
      journalId,
    }, true);
  }

  dispose(assetId: number, payload: {
    disposalDate: string;
    proceeds: number;
    accountingPeriodId: number;
    journalId: number;
    cashAccountId: number;
    gainAccountId: number;
    lossAccountId: number;
  }): Observable<unknown> {
    return this.api.post(`fixed-assets/${assetId}/dispose`, payload, true);
  }
}

function toPagedSetup(value: unknown): PagedFixedAssetSetup {
  const setup = toSetupWithoutAssets(value);
  if (!isRecord(value)) throw new Error('The fixed-assets response contains invalid setup data.');
  return {
    ...setup,
    assets: parsePagedResult(value['assets'], isFixedAsset, 'fixed assets'),
  };
}

function toSetup(value: unknown): FixedAssetSetup {
  if (!isRecord(value) ||
    typeof value['currencyCode'] !== 'string' ||
    !/^[A-Z]{3}$/.test(value['currencyCode']) ||
    !Array.isArray(value['assets']) || !value['assets'].every(isFixedAsset) ||
    !Array.isArray(value['periods']) || !value['periods'].every(isPeriod) ||
    !Array.isArray(value['journals']) || !value['journals'].every(isJournal) ||
    !Array.isArray(value['accounts']) || !value['accounts'].every(isAccount) ||
    !Array.isArray(value['depreciationHistory']) || !value['depreciationHistory'].every(isDepreciation)) {
    throw new Error('The fixed-assets response contains invalid setup data.');
  }
  return value as unknown as FixedAssetSetup;
}

function toSetupWithoutAssets(value: unknown): Omit<FixedAssetSetup, 'assets'> {
  if (!isRecord(value) ||
    typeof value['currencyCode'] !== 'string' ||
    !/^[A-Z]{3}$/.test(value['currencyCode']) ||
    !Array.isArray(value['periods']) || !value['periods'].every(isPeriod) ||
    !Array.isArray(value['journals']) || !value['journals'].every(isJournal) ||
    !Array.isArray(value['accounts']) || !value['accounts'].every(isAccount) ||
    !Array.isArray(value['depreciationHistory']) || !value['depreciationHistory'].every(isDepreciation)) {
    throw new Error('The fixed-assets response contains invalid setup data.');
  }
  return {
    currencyCode: value['currencyCode'],
    periods: value['periods'],
    journals: value['journals'],
    accounts: value['accounts'],
    depreciationHistory: value['depreciationHistory'],
  } as Omit<FixedAssetSetup, 'assets'>;
}

function isFixedAsset(value: unknown): value is FixedAsset {
  return isRecord(value) &&
    isPositiveId(value['id']) &&
    typeof value['assetNumber'] === 'string' &&
    typeof value['name'] === 'string' &&
    (value['description'] === null || typeof value['description'] === 'string') &&
    isDate(value['acquisitionDate']) &&
    isDate(value['inServiceDate']) &&
    isNumeric(value['acquisitionCost']) &&
    isNumeric(value['residualValue']) &&
    isPositiveId(value['usefulLifeMonths']) &&
    isPositiveId(value['assetAccountId']) &&
    isPositiveId(value['accumulatedDepreciationAccountId']) &&
    isPositiveId(value['depreciationExpenseAccountId']) &&
    isPositiveId(value['acquisitionJournalEntryId']) &&
    typeof value['status'] === 'string' &&
    (value['disposalDate'] === null || isDate(value['disposalDate'])) &&
    (value['disposalProceeds'] === null || isNumeric(value['disposalProceeds'])) &&
    (value['disposalJournalEntryId'] === null || isPositiveId(value['disposalJournalEntryId'])) &&
    isNumeric(value['accumulatedDepreciation']) &&
    isNumeric(value['netBookValue']) &&
    typeof value['assetAccountName'] === 'string' &&
    typeof value['accumulatedDepreciationAccountName'] === 'string' &&
    typeof value['depreciationExpenseAccountName'] === 'string';
}

function isPeriod(value: unknown): boolean {
  return isRecord(value) && isPositiveId(value['id']) &&
    isDate(value['startsOn']) && isDate(value['endsOn']) && typeof value['status'] === 'string';
}

function isJournal(value: unknown): boolean {
  return isRecord(value) && isPositiveId(value['id']) &&
    typeof value['code'] === 'string' && typeof value['name'] === 'string';
}

function isAccount(value: unknown): boolean {
  return isRecord(value) && isPositiveId(value['accountId']) &&
    typeof value['code'] === 'string' && typeof value['name'] === 'string' &&
    typeof value['accountType'] === 'string';
}

function isDepreciation(value: unknown): boolean {
  return isRecord(value) && isPositiveId(value['assetId']) &&
    isDate(value['depreciationDate']) && isNumeric(value['amount']) &&
    isPositiveId(value['journalEntryId']);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isPositiveId(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

function isNumeric(value: unknown): value is number {
  return typeof value === 'number' && Number.isFinite(value);
}

function isDate(value: unknown): value is string {
  return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value);
}
