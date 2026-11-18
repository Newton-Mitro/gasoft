import { Routes } from '@angular/router';

export const TAX_JURISDICTIONS_ROUTES: Routes = [
  { path: 'settings/tax-jurisdictions/create', redirectTo: '/settings/tax-jurisdictions', pathMatch: 'full' },
  { path: 'settings/tax-jurisdictions/:recordId/edit', redirectTo: '/settings/tax-jurisdictions' },
  { path: 'settings/tax-jurisdictions', loadComponent: () => import('./tax-jurisdictions-page/tax-jurisdictions.page').then(module => module.TaxJurisdictionsPageComponent), data: { page: 'tax-jurisdictions' } },
];
