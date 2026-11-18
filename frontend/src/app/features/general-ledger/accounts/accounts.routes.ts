import { Routes } from '@angular/router';

export const ACCOUNTS_ROUTES: Routes = [
  { path: 'general-ledger/accounts/create', loadComponent: () => import('./accounts-page/accounts.page').then(module => module.AccountsPageComponent), data: { page: 'accounts', view: 'create' } },
  { path: 'general-ledger/accounts/:recordId/edit', loadComponent: () => import('./accounts-page/accounts.page').then(module => module.AccountsPageComponent), data: { page: 'accounts', view: 'edit' } },
  { path: 'general-ledger/accounts', loadComponent: () => import('./accounts-page/accounts.page').then(module => module.AccountsPageComponent), data: { page: 'accounts' } },
];
