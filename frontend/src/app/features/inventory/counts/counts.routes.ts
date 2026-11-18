import { Routes } from '@angular/router';

export const COUNTS_ROUTES: Routes = [
  { path: 'inventory/counts/create', loadComponent: () => import('./inventory-counts-page/inventory-counts.page').then(module => module.InventoryCountsPageComponent), data: { page: 'inventory-counts', view: 'create' } },
  { path: 'inventory/counts/:recordId/edit', loadComponent: () => import('./inventory-counts-page/inventory-counts.page').then(module => module.InventoryCountsPageComponent), data: { page: 'inventory-counts', view: 'edit' } },
  { path: 'inventory/counts', loadComponent: () => import('./inventory-counts-page/inventory-counts.page').then(module => module.InventoryCountsPageComponent), data: { page: 'inventory-counts' } },
];
