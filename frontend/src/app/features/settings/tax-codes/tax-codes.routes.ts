import { Routes } from '@angular/router';

export const TAX_CODES_ROUTES: Routes = [
  { path: 'settings/tax-codes/create', loadComponent: () => import('./tax-codes-page/tax-codes.page').then(module => module.TaxCodesPageComponent), data: { page: 'tax-codes', view: 'create' } },
  { path: 'settings/tax-codes/:recordId/edit', loadComponent: () => import('./tax-codes-page/tax-codes.page').then(module => module.TaxCodesPageComponent), data: { page: 'tax-codes', view: 'edit' } },
  { path: 'settings/tax-codes', loadComponent: () => import('./tax-codes-page/tax-codes.page').then(module => module.TaxCodesPageComponent), data: { page: 'tax-codes' } },
];
