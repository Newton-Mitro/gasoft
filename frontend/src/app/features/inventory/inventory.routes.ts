import { Routes } from '@angular/router';

export const INVENTORY_ROUTES: Routes = [
  { path: 'inventory/items/create', loadComponent: () => import('./items/items.page').then(module => module.ItemsPageComponent), data: { page: 'items', view: 'create' } },
  { path: 'inventory/items/:recordId/edit', loadComponent: () => import('./items/items.page').then(module => module.ItemsPageComponent), data: { page: 'items', view: 'edit' } },
  { path: 'inventory/items', loadComponent: () => import('./items/items.page').then(module => module.ItemsPageComponent), data: { page: 'items' } },
  { path: 'inventory/categories/create', loadComponent: () => import('./item-categories/item-categories.page').then(module => module.ItemCategoriesPageComponent), data: { page: 'item-categories', view: 'create' } },
  { path: 'inventory/categories/:recordId/edit', loadComponent: () => import('./item-categories/item-categories.page').then(module => module.ItemCategoriesPageComponent), data: { page: 'item-categories', view: 'edit' } },
  { path: 'inventory/categories', loadComponent: () => import('./item-categories/item-categories.page').then(module => module.ItemCategoriesPageComponent), data: { page: 'item-categories' } },
  { path: 'inventory/warehouses/create', loadComponent: () => import('./warehouses/warehouses.page').then(module => module.WarehousesPageComponent), data: { page: 'warehouses', view: 'create' } },
  { path: 'inventory/warehouses/:recordId/edit', loadComponent: () => import('./warehouses/warehouses.page').then(module => module.WarehousesPageComponent), data: { page: 'warehouses', view: 'edit' } },
  { path: 'inventory/warehouses/:recordId/locations/create', loadComponent: () => import('./warehouses/warehouses.page').then(module => module.WarehousesPageComponent), data: { page: 'warehouses', view: 'location' } },
  { path: 'inventory/warehouses', loadComponent: () => import('./warehouses/warehouses.page').then(module => module.WarehousesPageComponent), data: { page: 'warehouses' } },
  { path: 'inventory/balances', loadComponent: () => import('./balances/inventory-balances.page').then(module => module.InventoryBalancesPageComponent), data: { page: 'inventory-balances' } },
  { path: 'inventory/counts/create', loadComponent: () => import('./counts/inventory-counts.page').then(module => module.InventoryCountsPageComponent), data: { page: 'inventory-counts', view: 'create' } },
  { path: 'inventory/counts/:recordId/edit', loadComponent: () => import('./counts/inventory-counts.page').then(module => module.InventoryCountsPageComponent), data: { page: 'inventory-counts', view: 'edit' } },
  { path: 'inventory/counts', loadComponent: () => import('./counts/inventory-counts.page').then(module => module.InventoryCountsPageComponent), data: { page: 'inventory-counts' } },
  { path: 'inventory/movements/receive', loadComponent: () => import('./movements/inventory-movements.page').then(module => module.InventoryMovementsPageComponent), data: { page: 'inventory-movements', view: 'receive' } },
  { path: 'inventory/movements/issue', loadComponent: () => import('./movements/inventory-movements.page').then(module => module.InventoryMovementsPageComponent), data: { page: 'inventory-movements', view: 'issue' } },
  { path: 'inventory/movements', loadComponent: () => import('./movements/inventory-movements.page').then(module => module.InventoryMovementsPageComponent), data: { page: 'inventory-movements' } },
];
