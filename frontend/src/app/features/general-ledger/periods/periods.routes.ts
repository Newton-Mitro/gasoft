import { Routes } from '@angular/router';

export const PERIODS_ROUTES: Routes = [
  { path: 'general-ledger/periods/create', loadComponent: () => import('./periods-page/periods.page').then(module => module.PeriodsPageComponent), data: { page: 'periods', view: 'create' } },
  { path: 'general-ledger/periods/:recordId/edit', loadComponent: () => import('./periods-page/periods.page').then(module => module.PeriodsPageComponent), data: { page: 'periods', view: 'edit' } },
  { path: 'general-ledger/periods', loadComponent: () => import('./periods-page/periods.page').then(module => module.PeriodsPageComponent), data: { page: 'periods' } },
];
