import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import {
  BankAccount,
  BankAccountPayload,
  BankAdjustmentPayload,
  BankImportResult,
  BankMatchCandidate,
  BankTransaction,
} from './bank-reconciliation.model';

const ENDPOINT = 'bank-accounts';

@Injectable({ providedIn: 'root' })
export class BankReconciliationService {
  constructor(private readonly api: ApiService) {}

  getAccounts(): Observable<BankAccount[]> {
    return this.api.get<unknown>(ENDPOINT).pipe(map(toBankAccounts));
  }

  createAccount(payload: BankAccountPayload): Observable<BankAccount> {
    return this.api.post<unknown>(ENDPOINT, payload).pipe(map(toBankAccount));
  }

  getTransactions(bankAccountId: number): Observable<BankTransaction[]> {
    return this.api.get<unknown>(`${ENDPOINT}/${bankAccountId}/transactions`).pipe(map(toBankTransactions));
  }

  importStatement(bankAccountId: number, file: File): Observable<BankImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.api.post<unknown>(`${ENDPOINT}/${bankAccountId}/statement`, formData).pipe(map(toBankImportResult));
  }

  getCandidates(bankAccountId: number, transactionId: number): Observable<BankMatchCandidate[]> {
    return this.api.get<unknown>(
      `${ENDPOINT}/${bankAccountId}/transactions/${transactionId}/matches`,
    ).pipe(map(toCandidates));
  }

  match(bankAccountId: number, transactionId: number, journalLineId: number): Observable<void> {
    return this.allocate(bankAccountId, transactionId, journalLineId, 0);
  }

  allocate(
    bankAccountId: number,
    transactionId: number,
    journalLineId: number,
    amount: number,
  ): Observable<void> {
    return this.api.post<void>(
      `${ENDPOINT}/${bankAccountId}/transactions/${transactionId}/match`,
      amount === 0 ? { journalLineId } : { journalLineId, amount },
    );
  }

  unmatch(bankAccountId: number, transactionId: number, matchId?: number): Observable<void> {
    return this.api.delete<void>(
      matchId
        ? `${ENDPOINT}/${bankAccountId}/transactions/${transactionId}/matches/${matchId}`
        : `${ENDPOINT}/${bankAccountId}/transactions/${transactionId}/match`,
    );
  }

  createAdjustment(
    bankAccountId: number,
    transactionId: number,
    payload: BankAdjustmentPayload,
  ): Observable<unknown> {
    return this.api.post(
      `${ENDPOINT}/${bankAccountId}/transactions/${transactionId}/adjustment`,
      payload,
    );
  }
}

function toBankAccounts(value: unknown): BankAccount[] {
  if (!Array.isArray(value) || !value.every(isBankAccount)) {
    throw new Error('The bank accounts response contains invalid records.');
  }
  return value;
}

function toBankAccount(value: unknown): BankAccount {
  if (!isBankAccount(value)) throw new Error('The bank account response contains an invalid record.');
  return value;
}

function toBankTransactions(value: unknown): BankTransaction[] {
  if (!Array.isArray(value) || !value.every(isBankTransaction)) {
    throw new Error('The bank transactions response contains invalid records.');
  }
  return value;
}

function toCandidates(value: unknown): BankMatchCandidate[] {
  if (!Array.isArray(value) || !value.every(isBankMatchCandidate)) {
    throw new Error('The bank matching response contains invalid records.');
  }
  return value;
}

function toBankImportResult(value: unknown): BankImportResult {
  if (!value || typeof value !== 'object') throw new Error('The statement import response is invalid.');
  const row = value as Record<string, unknown>;
  if (!isNonNegativeInteger(row['imported']) ||
      !isNonNegativeInteger(row['duplicates']) ||
      !isNonNegativeInteger(row['totalRows'])) {
    throw new Error('The statement import response is invalid.');
  }
  return {
    imported: row['imported'],
    duplicates: row['duplicates'],
    totalRows: row['totalRows'],
  };
}

function isBankAccount(value: unknown): value is BankAccount {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return isPositiveInteger(row['id']) &&
    typeof row['name'] === 'string' &&
    typeof row['bankName'] === 'string' &&
    typeof row['accountNumberLastFour'] === 'string' &&
    typeof row['currencyCode'] === 'string' &&
    isPositiveInteger(row['chartOfAccountId']) &&
    typeof row['chartOfAccountCode'] === 'string' &&
    typeof row['chartOfAccountName'] === 'string' &&
    typeof row['isActive'] === 'boolean';
}

function isBankTransaction(value: unknown): value is BankTransaction {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return isPositiveInteger(row['id']) &&
    typeof row['transactionDate'] === 'string' &&
    typeof row['description'] === 'string' &&
    (row['reference'] === null || typeof row['reference'] === 'string') &&
    typeof row['amount'] === 'number' &&
    typeof row['matchedAmount'] === 'number' &&
    typeof row['remainingAmount'] === 'number' &&
    Array.isArray(row['matches']) &&
    row['matches'].every(isBankReconciliationMatch);
}

function isBankReconciliationMatch(value: unknown): value is BankTransaction['matches'][number] {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return isPositiveInteger(row['id']) &&
    isPositiveInteger(row['journalLineId']) &&
    typeof row['amount'] === 'number' &&
    typeof row['entryDate'] === 'string' &&
    typeof row['description'] === 'string';
}

function isBankMatchCandidate(value: unknown): value is BankMatchCandidate {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return isPositiveInteger(row['journalLineId']) &&
    typeof row['entryDate'] === 'string' &&
    typeof row['description'] === 'string' &&
    typeof row['amount'] === 'number';
}

function isPositiveInteger(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

function isNonNegativeInteger(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value >= 0;
}
