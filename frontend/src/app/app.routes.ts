import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { Shell } from './layout/shell/shell';
import { GENERAL_LEDGER_ROUTES } from './features/general-ledger/general-ledger.routes';
import { BUSINESS_ROUTES } from './features/business/business.routes';
import { INVENTORY_ROUTES } from './features/inventory/inventory.routes';
import { SETTINGS_ROUTES } from './features/settings/settings.routes';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/auth/login/login.page').then(module => module.LoginPage) },
  { path: 'register', loadComponent: () => import('./features/auth/register/register.page').then(module => module.RegisterPage) },
  { path: 'forgot-password', loadComponent: () => import('./features/auth/forgot-password/forgot-password.page').then(module => module.ForgotPasswordPage) },
  { path: 'reset-password', loadComponent: () => import('./features/auth/reset-password/reset-password.page').then(module => module.ResetPasswordPage) },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.page').then(module => module.DashboardPage) },
      { path: 'account/profile', loadComponent: () => import('./features/account/profile/account-profile.page').then(module => module.AccountProfilePage) },
      ...GENERAL_LEDGER_ROUTES,
      ...BUSINESS_ROUTES,
      ...INVENTORY_ROUTES,
      ...SETTINGS_ROUTES,
    ],
  },
  { path: '**', redirectTo: '' },
];
