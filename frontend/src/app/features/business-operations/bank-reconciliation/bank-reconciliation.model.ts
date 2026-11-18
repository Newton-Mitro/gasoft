export interface BankAccount {
  id: number;
  name: string;
  bankName: string;
  accountNumberLastFour: string;
  currencyCode: string;
  chartOfAccountId: number;
  chartOfAccountCode: string;
  chartOfAccountName: string;
  isActive: boolean;
}

export interface BankAccountPayload {
  name: string;
  bankName: string;
  accountNumberLastFour: string;
  currencyCode: string;
  chartOfAccountId: number;
}

export interface BankTransaction {
  id: number;
  transactionDate: string;
  description: string;
  reference: string | null;
  amount: number;
  matchedAmount: number;
  remainingAmount: number;
  matches: BankReconciliationMatch[];
}

export interface BankReconciliationMatch {
  id: number;
  journalLineId: number;
  amount: number;
  entryDate: string;
  description: string;
}

export interface BankMatchCandidate {
  journalLineId: number;
  entryDate: string;
  description: string;
  amount: number;
}

export interface BankImportResult {
  imported: number;
  duplicates: number;
  totalRows: number;
}

export interface BankAdjustmentPayload {
  offsetAccountId: number;
  entryDate: string;
  description: string | null;
}
