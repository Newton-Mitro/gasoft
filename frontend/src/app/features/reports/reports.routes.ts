import { Routes } from '@angular/router';

export const REPORTS_ROUTES: Routes = [
  { path: 'reports/trial-balance', loadComponent: () => import('./statements/report-pages/report-pages.component').then(module => module.TrialBalancePageComponent), data: { page: 'trial-balance' } },
  { path: 'reports/income-statement', loadComponent: () => import('./statements/report-pages/report-pages.component').then(module => module.IncomeStatementPageComponent), data: { page: 'income-statement' } },
  { path: 'reports/balance-sheet', loadComponent: () => import('./statements/report-pages/report-pages.component').then(module => module.BalanceSheetPageComponent), data: { page: 'balance-sheet' } },
  { path: 'reports/budget-vs-actual', loadComponent: () => import('./budget/budget-page/budget.page').then(module => module.BudgetPageComponent), data: { page: 'budget-vs-actual' } },
  { path: 'reports/cash-flow', loadComponent: () => import('./cash-flow/cash-flow-page/cash-flow.page').then(module => module.CashFlowPageComponent), data: { page: 'cash-flow' } },
  { path: 'reports/receivables-aging', loadComponent: () => import('./aging/aging-report-page/aging-report.page').then(module => module.AgingReportPageComponent), data: { page: 'receivables-aging', agingKind: 'receivables' } },
  { path: 'reports/payables-aging', loadComponent: () => import('./aging/aging-report-page/aging-report.page').then(module => module.AgingReportPageComponent), data: { page: 'payables-aging', agingKind: 'payables' } },
  { path: 'reports/consolidation', loadComponent: () => import('./consolidation/consolidation-page/consolidation.page').then(module => module.ConsolidationPageComponent), data: { page: 'consolidation' } },
];
