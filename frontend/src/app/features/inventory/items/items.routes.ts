import { Routes } from '@angular/router';

export const ITEMS_ROUTES: Routes = [
  { path: 'inventory/items/create', loadComponent: () => import('./items-page/items.page').then(module => module.ItemsPageComponent), data: { page: 'items', view: 'create' } },
  { path: 'inventory/items/:recordId/edit', loadComponent: () => import('./items-page/items.page').then(module => module.ItemsPageComponent), data: { page: 'items', view: 'edit' } },
  { path: 'inventory/items', loadComponent: () => import('./items-page/items.page').then(module => module.ItemsPageComponent), data: { page: 'items' } },
];
