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

export interface CashFlowAccountBalance {
  accountId: number;
  accountCode: string;
  accountName: string;
  bankName: string;
  openingBalance: number;
  cashChange: number;
  closingBalance: number;
}

export interface CashFlowAccountFlow {
  accountId: number;
  accountCode: string;
  accountName: string;
  inflows: number;
  outflows: number;
  net: number;
}

export interface CashFlowSection {
  category: string;
  inflows: number;
  outflows: number;
  net: number;
  accounts: CashFlowAccountFlow[];
}

export interface CashFlowClassification {
  accountId: number;
  code: string;
  name: string;
  accountType: string;
  cashFlowCategory: string | null;
  isCashAccount: boolean;
}

export interface CashFlowReport {
  companyId: number;
  currencyCode: string;
  from: string;
  to: string;
  cashAccounts: CashFlowAccountBalance[];
  direct: {
    operating: CashFlowSection;
    investing: CashFlowSection;
    financing: CashFlowSection;
    unclassified: CashFlowSection;
    netCashIncrease: number;
  };
  indirect: {
    netIncome: number;
    nonCashAdjustments: number;
    workingCapitalAdjustments: number;
    operatingCashFlow: number;
    investingCashFlow: number;
    financingCashFlow: number;
    unclassifiedCashFlow: number;
    unclassifiedInflows: number;
    unclassifiedOutflows: number;
    netCashIncrease: number;
    actualCashMovement: number;
    reconciliationDifference: number;
  };
  openingBalance: number;
  closingBalance: number;
  actualCashMovement: number;
}

export interface AgingDocument {
  documentId: number;
  documentNumber: string;
  partyNumber: string;
  partyName: string;
  documentDate: string;
  dueDate: string;
  currencyCode: string;
  originalAmount: number;
  outstandingAmount: number;
  daysPastDue: number;
  agingBucket: 'Current' | '1-30 days' | '31-60 days' | '61-90 days' | 'Over 90 days';
}

export interface AgingCurrencyTotal {
  currencyCode: string;
  current: number;
  days1To30: number;
  days31To60: number;
  days61To90: number;
  over90Days: number;
  total: number;
}

export interface AgingReport {
  companyId: number;
  companyFunctionalCurrencyCode: string;
  asOfDate: string;
  documents: AgingDocument[];
  totalsByCurrency: AgingCurrencyTotal[];
}

export interface RetainedEarningsRollForwardReport {
  companyId: number;
  currencyCode: string;
  fiscalYearId: number;
  fiscalYearStart: string;
  fiscalYearEnd: string;
  retainedEarningsAccountId: number;
  retainedEarningsAccountCode: string;
  retainedEarningsAccountName: string;
  beginningRetainedEarnings: number;
  netIncomeForYear: number;
  ownerDrawsOrDividends: number;
  endingRetainedEarnings: number;
  closingBalanceVariance: number;
}

export type ReportType =
  | 'trial-balance'
  | 'income-statement'
  | 'balance-sheet';
export type ReportAccount = TrialBalanceAccount | IncomeStatementAccount | BalanceSheetAccount;
export type FinancialReport = TrialBalanceReport | IncomeStatementReport | BalanceSheetReport;

export interface ReportFilters {
  fromDate?: string;
  toDate?: string;
  asOfDate?: string;
}
