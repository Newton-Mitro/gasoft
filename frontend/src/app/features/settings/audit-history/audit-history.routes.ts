import { Routes } from '@angular/router';

export const AUDIT_HISTORY_ROUTES: Routes = [
  { path: 'settings/audit-history', loadComponent: () => import('./audit-history-page/audit-history.page').then(module => module.AuditHistoryPageComponent), data: { page: 'audit-history' } },
];
