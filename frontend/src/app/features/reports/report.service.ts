import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../core/api.service';
import {
  BalanceSheetReport,
  AgingReport,
  CashFlowReport,
  CashFlowClassification,
  IncomeStatementReport,
  ReportFilters,
  RetainedEarningsRollForwardReport,
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

  getRetainedEarningsRollForward(fiscalYearId: number): Observable<RetainedEarningsRollForwardReport> {
    const params = new HttpParams().set('fiscalYearId', fiscalYearId.toString());
    return this.api.get<unknown>('reports/retained-earnings', true, params)
      .pipe(map(toRetainedEarningsRollForwardReport));
  }

  getCashFlow(filters: ReportFilters): Observable<CashFlowReport> {
    let params = new HttpParams();
    if (filters.fromDate) params = params.set('fromDate', filters.fromDate);
    if (filters.toDate) params = params.set('toDate', filters.toDate);
    return this.api.get<unknown>('reports/cash-flow', true, params).pipe(map(toCashFlowReport));
  }

  getCashFlowClassifications(): Observable<CashFlowClassification[]> {
    return this.api.get<unknown>('reports/cash-flow/accounts', true).pipe(map(toCashFlowClassifications));
  }

  updateCashFlowClassification(accountId: number, category: string | null): Observable<unknown> {
    return this.api.put(`reports/cash-flow/accounts/${accountId}`, { category }, true);
  }

  getReceivablesAging(asOfDate: string): Observable<AgingReport> {
    return this.getAgingReport('reports/receivables-aging', asOfDate);
  }

  getPayablesAging(asOfDate: string): Observable<AgingReport> {
    return this.getAgingReport('reports/payables-aging', asOfDate);
  }

  private getAgingReport(endpoint: string, asOfDate: string): Observable<AgingReport> {
    const params = new HttpParams().set('asOfDate', asOfDate);
    return this.api.get<unknown>(endpoint, true, params).pipe(map(toAgingReport));
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

function toRetainedEarningsRollForwardReport(value: unknown): RetainedEarningsRollForwardReport {
  if (!isRecord(value) ||
    !isPositiveId(value['companyId']) ||
    !isCurrencyCode(value['currencyCode']) ||
    !isPositiveId(value['fiscalYearId']) ||
    !isDate(value['fiscalYearStart']) ||
    !isDate(value['fiscalYearEnd']) ||
    !isPositiveId(value['retainedEarningsAccountId']) ||
    typeof value['retainedEarningsAccountCode'] !== 'string' ||
    typeof value['retainedEarningsAccountName'] !== 'string' ||
    !isNumeric(value['beginningRetainedEarnings']) ||
    !isNumeric(value['netIncomeForYear']) ||
    !isNumeric(value['ownerDrawsOrDividends']) ||
    !isNumeric(value['endingRetainedEarnings']) ||
    !isNumeric(value['closingBalanceVariance'])) {
    throw new Error('The retained earnings roll-forward response contains invalid report data.');
  }
  return value as unknown as RetainedEarningsRollForwardReport;
}

function toCashFlowReport(value: unknown): CashFlowReport {
  if (!isRecord(value) ||
    !isPositiveId(value['companyId']) ||
    !isCurrencyCode(value['currencyCode']) ||
    !isDate(value['from']) ||
    !isDate(value['to']) ||
    !Array.isArray(value['cashAccounts']) ||
    !value['cashAccounts'].every(isCashFlowAccountBalance) ||
    !isDirectCashFlow(value['direct']) ||
    !isIndirectCashFlow(value['indirect']) ||
    !isNumeric(value['openingBalance']) ||
    !isNumeric(value['closingBalance']) ||
    !isNumeric(value['actualCashMovement'])) {
    throw new Error('The cash flow response contains invalid report data.');
  }
  return value as unknown as CashFlowReport;
}

function toCashFlowClassifications(value: unknown): CashFlowClassification[] {
  if (!Array.isArray(value) || !value.every(isCashFlowClassification)) {
    throw new Error('The cash flow classifications response contains invalid account data.');
  }
  return value as CashFlowClassification[];
}

function isCashFlowAccountBalance(value: unknown): boolean {
  return isRecord(value) &&
    isPositiveId(value['accountId']) &&
    typeof value['accountCode'] === 'string' &&
    typeof value['accountName'] === 'string' &&
    typeof value['bankName'] === 'string' &&
    isNumeric(value['openingBalance']) &&
    isNumeric(value['cashChange']) &&
    isNumeric(value['closingBalance']);
}

function isCashFlowAccountFlow(value: unknown): boolean {
  return isRecord(value) &&
    typeof value['accountId'] === 'number' && Number.isSafeInteger(value['accountId']) && value['accountId'] >= 0 &&
    typeof value['accountCode'] === 'string' &&
    typeof value['accountName'] === 'string' &&
    isNumeric(value['inflows']) &&
    isNumeric(value['outflows']) &&
    isNumeric(value['net']);
}

function isCashFlowSection(value: unknown): boolean {
  return isRecord(value) &&
    typeof value['category'] === 'string' &&
    isNumeric(value['inflows']) &&
    isNumeric(value['outflows']) &&
    isNumeric(value['net']) &&
    Array.isArray(value['accounts']) &&
    value['accounts'].every(isCashFlowAccountFlow);
}

function isDirectCashFlow(value: unknown): boolean {
  return isRecord(value) &&
    isCashFlowSection(value['operating']) &&
    isCashFlowSection(value['investing']) &&
    isCashFlowSection(value['financing']) &&
    isCashFlowSection(value['unclassified']) &&
    isNumeric(value['netCashIncrease']);
}

function isIndirectCashFlow(value: unknown): boolean {
  return isRecord(value) &&
    isNumeric(value['netIncome']) &&
    isNumeric(value['nonCashAdjustments']) &&
    isNumeric(value['workingCapitalAdjustments']) &&
    isNumeric(value['operatingCashFlow']) &&
    isNumeric(value['investingCashFlow']) &&
    isNumeric(value['financingCashFlow']) &&
    isNumeric(value['unclassifiedCashFlow']) &&
    isNumeric(value['unclassifiedInflows']) &&
    isNumeric(value['unclassifiedOutflows']) &&
    isNumeric(value['netCashIncrease']) &&
    isNumeric(value['actualCashMovement']) &&
    isNumeric(value['reconciliationDifference']);
}

function isCashFlowClassification(value: unknown): boolean {
  return isRecord(value) &&
    isPositiveId(value['accountId']) &&
    typeof value['code'] === 'string' &&
    typeof value['name'] === 'string' &&
    typeof value['accountType'] === 'string' &&
    (value['cashFlowCategory'] === null || typeof value['cashFlowCategory'] === 'string') &&
    typeof value['isCashAccount'] === 'boolean';
}

function toAgingReport(value: unknown): AgingReport {
  if (!isRecord(value) ||
    !isPositiveId(value['companyId']) ||
    !isCurrencyCode(value['companyFunctionalCurrencyCode']) ||
    !isDate(value['asOfDate']) ||
    !Array.isArray(value['documents']) ||
    !value['documents'].every(isAgingDocument) ||
    !Array.isArray(value['totalsByCurrency']) ||
    !value['totalsByCurrency'].every(isAgingCurrencyTotal)) {
    throw new Error('The aging report response contains invalid report data.');
  }
  return value as unknown as AgingReport;
}

function isAgingDocument(value: unknown): boolean {
  return isRecord(value) &&
    isPositiveId(value['documentId']) &&
    typeof value['documentNumber'] === 'string' &&
    typeof value['partyNumber'] === 'string' &&
    typeof value['partyName'] === 'string' &&
    isDate(value['documentDate']) &&
    isDate(value['dueDate']) &&
    isCurrencyCode(value['currencyCode']) &&
    isNumeric(value['originalAmount']) &&
    isNumeric(value['outstandingAmount']) &&
    Number.isSafeInteger(value['daysPastDue']) &&
    ['Current', '1-30 days', '31-60 days', '61-90 days', 'Over 90 days'].includes(String(value['agingBucket']));
}

function isAgingCurrencyTotal(value: unknown): boolean {
  return isRecord(value) &&
    isCurrencyCode(value['currencyCode']) &&
    isNumeric(value['current']) &&
    isNumeric(value['days1To30']) &&
    isNumeric(value['days31To60']) &&
    isNumeric(value['days61To90']) &&
    isNumeric(value['over90Days']) &&
    isNumeric(value['total']);
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
