export interface JournalEntryLine {
  id: number;
  accountId: number;
  transactionCurrencyCode: string;
  debit: number | string;
  credit: number | string;
  functionalDebit: number | string;
  functionalCredit: number | string;
  exchangeRate: number | string;
  description: string | null;
}

export interface JournalEntry {
  id: number;
  companyId: number;
  journalId: number;
  accountingPeriodId: number;
  entryDate: string;
  description: string;
  status: string;
  totalDebit: number | string;
  totalCredit: number | string;
  reversalOfEntryId?: number | null;
  reversedByEntryId?: number | null;
  lines: JournalEntryLine[];
}

export interface JournalEntryLinePayload {
  accountId: number;
  transactionCurrencyCode: string;
  debit: number;
  credit: number;
  exchangeRate: number;
  description: string;
}

export interface JournalEntryPayload {
  journalId: number;
  accountingPeriodId: number;
  entryDate: string;
  description: string;
  lines: JournalEntryLinePayload[];
}

export interface JournalEntryPostResult {
  id: number;
  status: string;
  [key: string]: unknown;
}
