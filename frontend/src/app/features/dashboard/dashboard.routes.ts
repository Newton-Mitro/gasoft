import { Routes } from '@angular/router';

export const DASHBOARD_ROUTES: Routes = [
  { path: 'dashboard', loadComponent: () => import('./dashboard-page/dashboard.page').then(module => module.DashboardPage) },
];
