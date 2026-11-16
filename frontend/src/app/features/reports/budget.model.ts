export interface BudgetFiscalYear {
  id: number;
  startsOn: string;
  endsOn: string;
  status: string;
}

export interface BudgetChoice {
  id: number;
  name: string;
  fiscalYearId: number;
}

export interface BudgetPeriod {
  id: number;
  startsOn: string;
  endsOn: string;
  status: string;
}

export interface BudgetAccount {
  accountId: number;
  code: string;
  name: string;
  accountType: string;
}

export interface BudgetSetup {
  fiscalYears: BudgetFiscalYear[];
  budgets: BudgetChoice[];
  periods: BudgetPeriod[];
  accounts: BudgetAccount[];
}

export interface BudgetLineInput {
  accountId: number;
  accountingPeriodId: number;
  amount: number;
}

export interface BudgetReportPeriodValue {
  periodId: number;
  budget: number;
  actual: number;
  variance: number;
  favorableVariance: number;
}

export interface BudgetReportAccount {
  accountId: number;
  accountCode: string;
  accountName: string;
  accountType: string;
  periods: BudgetReportPeriodValue[];
  budget: number;
  actual: number;
  favorableVariance: number;
}

export interface BudgetVsActualReport {
  budget: BudgetChoice;
  currencyCode: string;
  periods: BudgetPeriod[];
  accounts: BudgetReportAccount[];
  summary: {
    revenueBudget: number;
    revenueActual: number;
    expenseBudget: number;
    expenseActual: number;
    netIncomeBudget: number;
    netIncomeActual: number;
  };
}
