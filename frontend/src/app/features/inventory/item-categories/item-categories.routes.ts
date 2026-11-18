import { Routes } from '@angular/router';

export const ITEM_CATEGORIES_ROUTES: Routes = [
  { path: 'inventory/categories/create', loadComponent: () => import('./item-categories-page/item-categories.page').then(module => module.ItemCategoriesPageComponent), data: { page: 'item-categories', view: 'create' } },
  { path: 'inventory/categories/:recordId/edit', loadComponent: () => import('./item-categories-page/item-categories.page').then(module => module.ItemCategoriesPageComponent), data: { page: 'item-categories', view: 'edit' } },
  { path: 'inventory/categories', loadComponent: () => import('./item-categories-page/item-categories.page').then(module => module.ItemCategoriesPageComponent), data: { page: 'item-categories' } },
];
