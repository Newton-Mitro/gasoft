import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { Shell } from './layout/shell/shell';
import { AUTH_ROUTES } from './features/auth/auth.routes';
import { DASHBOARD_ROUTES } from './features/dashboard/dashboard.routes';
import { PROFILE_ROUTES } from './features/profile/profile.routes';
import { GENERAL_LEDGER_ROUTES } from './features/general-ledger/general-ledger.routes';
import { BUSINESS_OPERATIONS_ROUTES } from './features/business-operations/business-operations.routes';
import { INVENTORY_ROUTES } from './features/inventory/inventory.routes';
import { SETTINGS_ROUTES } from './features/settings/settings.routes';

export const routes: Routes = [
  ...AUTH_ROUTES,
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      ...DASHBOARD_ROUTES,
      ...PROFILE_ROUTES,
      ...GENERAL_LEDGER_ROUTES,
      ...BUSINESS_OPERATIONS_ROUTES,
      ...INVENTORY_ROUTES,
      ...SETTINGS_ROUTES,
    ],
  },
  { path: '**', redirectTo: '' },
];
