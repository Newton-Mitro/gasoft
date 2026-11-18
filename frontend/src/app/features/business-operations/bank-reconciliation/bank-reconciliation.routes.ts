import { Routes } from '@angular/router';

export const BANK_RECONCILIATION_ROUTES: Routes = [
  { path: 'banking/reconciliation', loadComponent: () => import('./bank-reconciliation-page/bank-reconciliation.page').then(module => module.BankReconciliationPageComponent), data: { page: 'bank-reconciliation' } },
];
