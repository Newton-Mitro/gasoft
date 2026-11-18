import { Routes } from '@angular/router';

export const PURCHASE_ORDERS_ROUTES: Routes = [
  { path: 'purchase-orders/create', loadComponent: () => import('./purchase-orders-page/purchase-orders.page').then(module => module.PurchaseOrdersPageComponent), data: { page: 'purchase-orders', view: 'create' } },
  { path: 'purchase-orders', loadComponent: () => import('./purchase-orders-page/purchase-orders.page').then(module => module.PurchaseOrdersPageComponent), data: { page: 'purchase-orders' } },
];
