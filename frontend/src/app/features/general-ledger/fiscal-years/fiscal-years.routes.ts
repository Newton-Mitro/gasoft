import { Routes } from '@angular/router';

export const FISCAL_YEARS_ROUTES: Routes = [
  { path: 'general-ledger/fiscal-years/create', loadComponent: () => import('./fiscal-years-page/fiscal-years.page').then(module => module.FiscalYearsPageComponent), data: { page: 'fiscal-years', view: 'create' } },
  { path: 'general-ledger/fiscal-years/:recordId/edit', loadComponent: () => import('./fiscal-years-page/fiscal-years.page').then(module => module.FiscalYearsPageComponent), data: { page: 'fiscal-years', view: 'edit' } },
  { path: 'general-ledger/fiscal-years', loadComponent: () => import('./fiscal-years-page/fiscal-years.page').then(module => module.FiscalYearsPageComponent), data: { page: 'fiscal-years' } },
];
