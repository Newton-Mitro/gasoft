import { Routes } from '@angular/router';

export const TAX_REGISTRATIONS_ROUTES: Routes = [
  { path: 'settings/tax-registrations/create', loadComponent: () => import('./tax-registrations-page/tax-registrations.page').then(module => module.TaxRegistrationsPageComponent), data: { page: 'tax-registrations', view: 'create' } },
  { path: 'settings/tax-registrations/:recordId/edit', loadComponent: () => import('./tax-registrations-page/tax-registrations.page').then(module => module.TaxRegistrationsPageComponent), data: { page: 'tax-registrations', view: 'edit' } },
  { path: 'settings/tax-registrations', loadComponent: () => import('./tax-registrations-page/tax-registrations.page').then(module => module.TaxRegistrationsPageComponent), data: { page: 'tax-registrations' } },
];
