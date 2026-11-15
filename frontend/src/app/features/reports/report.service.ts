import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../core/api.service';
import {
  BalanceSheetReport,
  IncomeStatementReport,
  ReportFilters,
  TrialBalanceReport,
} from './report.model';

@Injectable({ providedIn: 'root' })
export class FinancialReportService {
  constructor(private readonly api: ApiService) {}

  getTrialBalance(filters: ReportFilters): Observable<TrialBalanceReport> {
    let params = new HttpParams();
    if (filters.fromDate) params = params.set('fromDate', filters.fromDate);
    if (filters.toDate) params = params.set('toDate', filters.toDate);
    return this.api.get<unknown>('reports/trial-balance', true, params).pipe(map(toTrialBalanceReport));
  }

  getIncomeStatement(filters: ReportFilters): Observable<IncomeStatementReport> {
    let params = new HttpParams();
    if (filters.fromDate) params = params.set('fromDate', filters.fromDate);
    if (filters.toDate) params = params.set('toDate', filters.toDate);
    return this.api.get<unknown>('reports/income-statement', true, params).pipe(map(toIncomeStatementReport));
  }

  getBalanceSheet(filters: ReportFilters): Observable<BalanceSheetReport> {
    let params = new HttpParams();
    if (filters.asOfDate) params = params.set('asOfDate', filters.asOfDate);
    return this.api.get<unknown>('reports/balance-sheet', true, params).pipe(map(toBalanceSheetReport));
  }
}

function toTrialBalanceReport(value: unknown): TrialBalanceReport {
  if (!isRecord(value) ||
    !isPositiveId(value['companyId']) ||
    !isCurrencyCode(value['currencyCode']) ||
    !isNullableDate(value['from']) ||
    !isNullableDate(value['to']) ||
    !Array.isArray(value['accounts']) ||
    !value['accounts'].every(isTrialBalanceAccount) ||
    !isNumeric(value['totalDebit']) ||
    !isNumeric(value['totalCredit'])) {
    throw new Error('The trial balance response contains invalid report data.');
  }
  return value as unknown as TrialBalanceReport;
}

function toIncomeStatementReport(value: unknown): IncomeStatementReport {
  if (!isRecord(value) ||
    !isPositiveId(value['companyId']) ||
    !isCurrencyCode(value['currencyCode']) ||
    !isNullableDate(value['from']) ||
    !isNullableDate(value['to']) ||
    !Array.isArray(value['accounts']) ||
    !value['accounts'].every(isIncomeStatementAccount) ||
    !isNumeric(value['totalRevenue']) ||
    !isNumeric(value['totalExpenses']) ||
    !isNumeric(value['netIncome'])) {
    throw new Error('The income statement response contains invalid report data.');
  }
  return value as unknown as IncomeStatementReport;
}

function toBalanceSheetReport(value: unknown): BalanceSheetReport {
  if (!isRecord(value) ||
    !isPositiveId(value['companyId']) ||
    !isCurrencyCode(value['currencyCode']) ||
    !isDate(value['asOfDate']) ||
    !Array.isArray(value['accounts']) ||
    !value['accounts'].every(isBalanceSheetAccount) ||
    !isNumeric(value['totalAssets']) ||
    !isNumeric(value['totalLiabilities']) ||
    !isNumeric(value['totalEquity']) ||
    !isNumeric(value['currentEarnings']) ||
    !isNumeric(value['totalLiabilitiesAndEquity']) ||
    !isNumeric(value['balanceDifference'])) {
    throw new Error('The balance sheet response contains invalid report data.');
  }
  return value as unknown as BalanceSheetReport;
}

function isTrialBalanceAccount(value: unknown): boolean {
  return isReportAccount(value) && isNumeric(value['debit']) && isNumeric(value['credit']);
}

function isIncomeStatementAccount(value: unknown): boolean {
  return isReportAccount(value) && isNumeric(value['amount']);
}

function isBalanceSheetAccount(value: unknown): boolean {
  return isReportAccount(value) && isNumeric(value['balance']);
}

function isReportAccount(value: unknown): value is Record<string, unknown> {
  return isRecord(value) &&
    (value['accountId'] === null || isPositiveId(value['accountId'])) &&
    typeof value['accountCode'] === 'string' &&
    typeof value['accountName'] === 'string' &&
    typeof value['accountType'] === 'string';
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value && typeof value === 'object' && !Array.isArray(value));
}

function isPositiveId(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

function isNumeric(value: unknown): value is number {
  return typeof value === 'number' && Number.isFinite(value);
}

function isCurrencyCode(value: unknown): value is string {
  return typeof value === 'string' && /^[A-Z]{3}$/.test(value);
}

function isNullableDate(value: unknown): value is string | null {
  return value === null || isDate(value);
}

function isDate(value: unknown): value is string {
  return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value);
}
