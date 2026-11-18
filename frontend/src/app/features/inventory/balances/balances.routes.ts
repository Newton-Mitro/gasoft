import { Routes } from '@angular/router';

export const BALANCES_ROUTES: Routes = [
  { path: 'inventory/balances', loadComponent: () => import('./inventory-balances-page/inventory-balances.page').then(module => module.InventoryBalancesPageComponent), data: { page: 'inventory-balances' } },
];
