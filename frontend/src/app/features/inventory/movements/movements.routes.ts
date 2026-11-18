import { Routes } from '@angular/router';

export const MOVEMENTS_ROUTES: Routes = [
  { path: 'inventory/movements/receive', loadComponent: () => import('./inventory-movements-page/inventory-movements.page').then(module => module.InventoryMovementsPageComponent), data: { page: 'inventory-movements', view: 'receive' } },
  { path: 'inventory/movements/issue', loadComponent: () => import('./inventory-movements-page/inventory-movements.page').then(module => module.InventoryMovementsPageComponent), data: { page: 'inventory-movements', view: 'issue' } },
  { path: 'inventory/movements', loadComponent: () => import('./inventory-movements-page/inventory-movements.page').then(module => module.InventoryMovementsPageComponent), data: { page: 'inventory-movements' } },
];
