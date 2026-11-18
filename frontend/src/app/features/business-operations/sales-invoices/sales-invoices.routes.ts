import { Routes } from '@angular/router';

export const SALES_INVOICES_ROUTES: Routes = [
  { path: 'sales-invoices/create', loadComponent: () => import('./sales-invoices-page/sales-invoices.page').then(module => module.SalesInvoicesPageComponent), data: { page: 'sales-invoices', view: 'create' } },
  { path: 'sales-invoices/:recordId/edit', loadComponent: () => import('./sales-invoices-page/sales-invoices.page').then(module => module.SalesInvoicesPageComponent), data: { page: 'sales-invoices', view: 'edit' } },
  { path: 'sales-invoices', loadComponent: () => import('./sales-invoices-page/sales-invoices.page').then(module => module.SalesInvoicesPageComponent), data: { page: 'sales-invoices' } },
];
