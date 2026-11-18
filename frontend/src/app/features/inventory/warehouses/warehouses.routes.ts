import { Routes } from '@angular/router';

export const WAREHOUSES_ROUTES: Routes = [
  { path: 'inventory/warehouses/create', loadComponent: () => import('./warehouses-page/warehouses.page').then(module => module.WarehousesPageComponent), data: { page: 'warehouses', view: 'create' } },
  { path: 'inventory/warehouses/:recordId/edit', loadComponent: () => import('./warehouses-page/warehouses.page').then(module => module.WarehousesPageComponent), data: { page: 'warehouses', view: 'edit' } },
  { path: 'inventory/warehouses/:recordId/locations/create', loadComponent: () => import('./warehouses-page/warehouses.page').then(module => module.WarehousesPageComponent), data: { page: 'warehouses', view: 'location' } },
  { path: 'inventory/warehouses', loadComponent: () => import('./warehouses-page/warehouses.page').then(module => module.WarehousesPageComponent), data: { page: 'warehouses' } },
];
