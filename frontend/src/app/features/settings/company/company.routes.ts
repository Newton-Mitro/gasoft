import { Routes } from '@angular/router';

export const COMPANY_ROUTES: Routes = [
  { path: 'settings/company/create', loadComponent: () => import('./company-settings-page/company-settings.page').then(module => module.CompanySettingsPageComponent), data: { page: 'company', view: 'create' } },
  { path: 'settings/company/edit', loadComponent: () => import('./company-settings-page/company-settings.page').then(module => module.CompanySettingsPageComponent), data: { page: 'company', view: 'edit' } },
  { path: 'settings/company', loadComponent: () => import('./company-settings-page/company-settings.page').then(module => module.CompanySettingsPageComponent), data: { page: 'company' } },
];
