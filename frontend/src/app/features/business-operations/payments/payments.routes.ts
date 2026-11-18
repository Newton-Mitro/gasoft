import { Routes } from '@angular/router';

export const PAYMENTS_ROUTES: Routes = [
  { path: 'payments/create', loadComponent: () => import('./payments-page/payments.page').then(module => module.PaymentsPageComponent), data: { page: 'payments', view: 'create' } },
  { path: 'payments/:recordId/edit', loadComponent: () => import('./payments-page/payments.page').then(module => module.PaymentsPageComponent), data: { page: 'payments', view: 'edit' } },
  { path: 'payments', loadComponent: () => import('./payments-page/payments.page').then(module => module.PaymentsPageComponent), data: { page: 'payments' } },
];
