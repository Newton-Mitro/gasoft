export interface ConsolidationCompany {
  id: number;
  code: string;
  legalName: string;
  functionalCurrencyCode: string;
}

export interface ConsolidationGroup {
  id: number;
  code: string;
  name: string;
  reportingCurrencyCode: string;
  companies: ConsolidationCompany[];
}

export interface ConsolidationRunSummary {
  id: number;
  periodEnd: string;
  status: string;
}

export interface ConsolidatedAccount {
  accountCode: string;
  accountName: string;
  debit: number;
  credit: number;
}

export interface ConsolidationRun {
  id: number;
  periodStart: string;
  periodEnd: string;
  status: string;
  reportingCurrencyCode: string;
  accounts: ConsolidatedAccount[];
  translationRates: ConsolidationTranslationRate[];
  eliminations: ConsolidationElimination[];
  translationAdjustments: ConsolidationElimination[];
}

export interface ConsolidationTranslationRate {
  companyId: number;
  sourceCurrencyCode: string;
  targetCurrencyCode: string;
  closingRate: number;
  averageRate: number;
}

export interface ConsolidationElimination {
  accountCode: string;
  accountName: string;
  debit: number;
  credit: number;
  description: string | null;
}

export interface ConsolidationRateInput {
  companyId: number;
  closingRate: number;
  averageRate: number;
}

export interface ConsolidationGroupPayload {
  code: string;
  name: string;
  reportingCurrencyCode: string;
  companyIds: number[];
}
