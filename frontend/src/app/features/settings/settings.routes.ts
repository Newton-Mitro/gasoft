import { Routes } from '@angular/router';
export const SETTINGS_ROUTES: Routes = [
  { path: 'settings/tax-jurisdictions/create', redirectTo: '/settings/tax-jurisdictions', pathMatch: 'full' },
  { path: 'settings/tax-jurisdictions/:recordId/edit', redirectTo: '/settings/tax-jurisdictions' },
  { path: 'settings/tax-jurisdictions', loadComponent: () => import('./tax-jurisdictions/tax-jurisdictions.page').then(module => module.TaxJurisdictionsPageComponent), data: { page: 'tax-jurisdictions' } },
  { path: 'settings/tax-codes/create', loadComponent: () => import('./tax-codes/tax-codes.page').then(module => module.TaxCodesPageComponent), data: { page: 'tax-codes', view: 'create' } },
  { path: 'settings/tax-codes/:recordId/edit', loadComponent: () => import('./tax-codes/tax-codes.page').then(module => module.TaxCodesPageComponent), data: { page: 'tax-codes', view: 'edit' } },
  { path: 'settings/tax-codes', loadComponent: () => import('./tax-codes/tax-codes.page').then(module => module.TaxCodesPageComponent), data: { page: 'tax-codes' } },
  { path: 'settings/tax-registrations/create', loadComponent: () => import('./tax-registrations/tax-registrations.page').then(module => module.TaxRegistrationsPageComponent), data: { page: 'tax-registrations', view: 'create' } },
  { path: 'settings/tax-registrations/:recordId/edit', loadComponent: () => import('./tax-registrations/tax-registrations.page').then(module => module.TaxRegistrationsPageComponent), data: { page: 'tax-registrations', view: 'edit' } },
  { path: 'settings/tax-registrations', loadComponent: () => import('./tax-registrations/tax-registrations.page').then(module => module.TaxRegistrationsPageComponent), data: { page: 'tax-registrations' } },
  { path: 'settings/company/create', loadComponent: () => import('./company/company-settings.page').then(module => module.CompanySettingsPageComponent), data: { page: 'company', view: 'create' } },
  { path: 'settings/company/edit', loadComponent: () => import('./company/company-settings.page').then(module => module.CompanySettingsPageComponent), data: { page: 'company', view: 'edit' } },
  { path: 'settings/company', loadComponent: () => import('./company/company-settings.page').then(module => module.CompanySettingsPageComponent), data: { page: 'company' } },
  { path: 'users/create', loadComponent: () => import('./users/users.page').then(module => module.UsersPageComponent), data: { page: 'users', view: 'create' } },
  { path: 'users/:recordId/edit', loadComponent: () => import('./users/users.page').then(module => module.UsersPageComponent), data: { page: 'users', view: 'edit' } },
  { path: 'users', loadComponent: () => import('./users/users.page').then(module => module.UsersPageComponent), data: { page: 'users' } },
];
