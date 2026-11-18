import { Routes } from '@angular/router';

export const PURCHASE_BILLS_ROUTES: Routes = [
  { path: 'purchase-bills/create', loadComponent: () => import('./purchase-bills-page/purchase-bills.page').then(module => module.PurchaseBillsPageComponent), data: { page: 'purchase-bills', view: 'create' } },
  { path: 'purchase-bills/:recordId/edit', loadComponent: () => import('./purchase-bills-page/purchase-bills.page').then(module => module.PurchaseBillsPageComponent), data: { page: 'purchase-bills', view: 'edit' } },
  { path: 'purchase-bills', loadComponent: () => import('./purchase-bills-page/purchase-bills.page').then(module => module.PurchaseBillsPageComponent), data: { page: 'purchase-bills' } },
];
