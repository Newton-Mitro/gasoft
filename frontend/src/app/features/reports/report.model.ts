export interface ReportAccountBase {
  accountId: number | null;
  accountCode: string;
  accountName: string;
  accountType: string;
}

export interface TrialBalanceAccount extends ReportAccountBase {
  debit: number;
  credit: number;
}

export interface TrialBalanceReport {
  companyId: number;
  currencyCode: string;
  from: string | null;
  to: string | null;
  accounts: TrialBalanceAccount[];
  totalDebit: number;
  totalCredit: number;
}

export interface IncomeStatementAccount extends ReportAccountBase {
  amount: number;
}

export interface IncomeStatementReport {
  companyId: number;
  currencyCode: string;
  from: string | null;
  to: string | null;
  accounts: IncomeStatementAccount[];
  totalRevenue: number;
  totalExpenses: number;
  netIncome: number;
}

export interface BalanceSheetAccount extends ReportAccountBase {
  balance: number;
}

export interface BalanceSheetReport {
  companyId: number;
  currencyCode: string;
  asOfDate: string;
  accounts: BalanceSheetAccount[];
  totalAssets: number;
  totalLiabilities: number;
  totalEquity: number;
  currentEarnings: number;
  totalLiabilitiesAndEquity: number;
  balanceDifference: number;
}

export type ReportType = 'trial-balance' | 'income-statement' | 'balance-sheet';
export type ReportAccount = TrialBalanceAccount | IncomeStatementAccount | BalanceSheetAccount;
export type FinancialReport = TrialBalanceReport | IncomeStatementReport | BalanceSheetReport;

export interface ReportFilters {
  fromDate?: string;
  toDate?: string;
  asOfDate?: string;
}
