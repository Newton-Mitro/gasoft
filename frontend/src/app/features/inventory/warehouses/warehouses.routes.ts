import { Routes } from '@angular/router';

export const WAREHOUSES_ROUTES: Routes = [
  { path: 'inventory/warehouses/create', loadComponent: () => import('./warehouses-page/warehouses.page').then(module => module.WarehousesPageComponent), data: { page: 'warehouses', view: 'create' } },
  { path: 'inventory/warehouses/:recordId/locations/:locationId/edit', loadComponent: () => import('./warehouses-page/warehouses.page').then(module => module.WarehousesPageComponent), data: { page: 'warehouses', view: 'location-edit' } },
  { path: 'inventory/warehouses/:recordId/locations', loadComponent: () => import('./warehouses-page/warehouses.page').then(module => module.WarehousesPageComponent), data: { page: 'warehouses', view: 'locations' } },
  { path: 'inventory/warehouses/:recordId/edit', loadComponent: () => import('./warehouses-page/warehouses.page').then(module => module.WarehousesPageComponent), data: { page: 'warehouses', view: 'edit' } },
  { path: 'inventory/warehouses/:recordId/locations/create', loadComponent: () => import('./warehouses-page/warehouses.page').then(module => module.WarehousesPageComponent), data: { page: 'warehouses', view: 'location' } },
  { path: 'inventory/warehouses', loadComponent: () => import('./warehouses-page/warehouses.page').then(module => module.WarehousesPageComponent), data: { page: 'warehouses' } },
];
