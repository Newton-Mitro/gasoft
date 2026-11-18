import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import {
  BudgetChoice,
  BudgetLineInput,
  BudgetSetup,
  BudgetVsActualReport,
} from './budget.model';

@Injectable({ providedIn: 'root' })
export class BudgetService {
  constructor(private readonly api: ApiService) {}

  getSetup(fiscalYearId?: number): Observable<BudgetSetup> {
    let params = new HttpParams();
    if (fiscalYearId !== undefined) params = params.set('fiscalYearId', fiscalYearId);
    return this.api.get<unknown>('budgets', true, params).pipe(map(toBudgetSetup));
  }

  create(name: string, fiscalYearId: number): Observable<BudgetChoice> {
    return this.api.post<unknown>('budgets', { name, fiscalYearId }, true).pipe(map(toBudgetChoice));
  }

  saveLines(budgetId: number, lines: BudgetLineInput[]): Observable<void> {
    return this.api.put<void>(`budgets/${budgetId}/lines`, { lines }, true);
  }

  delete(budgetId: number): Observable<void> {
    return this.api.delete<void>(`budgets/${budgetId}`, true);
  }

  getBudgetVsActual(budgetId: number): Observable<BudgetVsActualReport> {
    const params = new HttpParams().set('budgetId', budgetId);
    return this.api.get<unknown>('reports/budget-vs-actual', true, params).pipe(map(toBudgetVsActualReport));
  }
}

function toBudgetSetup(value: unknown): BudgetSetup {
  if (!isRecord(value) ||
    !Array.isArray(value['fiscalYears']) || !value['fiscalYears'].every(isFiscalYear) ||
    !Array.isArray(value['budgets']) || !value['budgets'].every(isBudgetChoice) ||
    !Array.isArray(value['periods']) || !value['periods'].every(isPeriod) ||
    !Array.isArray(value['accounts']) || !value['accounts'].every(isBudgetAccount)) {
    throw new Error('The budgets response contains invalid setup data.');
  }
  return value as unknown as BudgetSetup;
}

function toBudgetChoice(value: unknown): BudgetChoice {
  if (!isBudgetChoice(value)) throw new Error('The created budget response contains invalid data.');
  return value;
}

function toBudgetVsActualReport(value: unknown): BudgetVsActualReport {
  const summary = isRecord(value) ? value['summary'] : null;
  if (!isRecord(value) ||
    !isBudgetChoice(value['budget']) ||
    !isCurrencyCode(value['currencyCode']) ||
    !Array.isArray(value['periods']) || !value['periods'].every(isPeriod) ||
    !Array.isArray(value['accounts']) || !value['accounts'].every(isReportAccount) ||
    !isRecord(summary) ||
    ![
      'revenueBudget', 'revenueActual', 'expenseBudget', 'expenseActual',
      'netIncomeBudget', 'netIncomeActual',
    ].every(key => isNumeric(summary[key]))) {
    throw new Error('The budget-vs-actual response contains invalid report data.');
  }
  return value as unknown as BudgetVsActualReport;
}

function isFiscalYear(value: unknown): boolean {
  return isRecord(value) && isPositiveId(value['id']) &&
    isDate(value['startsOn']) && isDate(value['endsOn']) && typeof value['status'] === 'string';
}

function isBudgetChoice(value: unknown): value is BudgetChoice {
  return isRecord(value) && isPositiveId(value['id']) &&
    typeof value['name'] === 'string' && isPositiveId(value['fiscalYearId']);
}

function isPeriod(value: unknown): boolean {
  return isRecord(value) && isPositiveId(value['id']) &&
    isDate(value['startsOn']) && isDate(value['endsOn']) && typeof value['status'] === 'string';
}

function isBudgetAccount(value: unknown): boolean {
  return isRecord(value) && isPositiveId(value['accountId']) &&
    typeof value['code'] === 'string' && typeof value['name'] === 'string' &&
    (value['accountType'] === 'Revenue' || value['accountType'] === 'Expense');
}

function isReportAccount(value: unknown): boolean {
  return isRecord(value) && isPositiveId(value['accountId']) &&
    typeof value['accountCode'] === 'string' && typeof value['accountName'] === 'string' &&
    (value['accountType'] === 'Revenue' || value['accountType'] === 'Expense') &&
    Array.isArray(value['periods']) && value['periods'].every(isPeriodValue) &&
    isNumeric(value['budget']) && isNumeric(value['actual']) && isNumeric(value['favorableVariance']);
}

function isPeriodValue(value: unknown): boolean {
  return isRecord(value) && isPositiveId(value['periodId']) &&
    isNumeric(value['budget']) && isNumeric(value['actual']) &&
    isNumeric(value['variance']) && isNumeric(value['favorableVariance']);
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

function isCurrencyCode(value: unknown): value is string {
  return typeof value === 'string' && /^[A-Z]{3}$/.test(value);
}
