import { Routes } from '@angular/router';
import { ITEMS_ROUTES } from './items/items.routes';
import { ITEM_CATEGORIES_ROUTES } from './item-categories/item-categories.routes';
import { WAREHOUSES_ROUTES } from './warehouses/warehouses.routes';
import { BALANCES_ROUTES } from './balances/balances.routes';
import { COUNTS_ROUTES } from './counts/counts.routes';
import { MOVEMENTS_ROUTES } from './movements/movements.routes';

export const INVENTORY_ROUTES: Routes = [
  ...ITEMS_ROUTES,
  ...ITEM_CATEGORIES_ROUTES,
  ...WAREHOUSES_ROUTES,
  ...BALANCES_ROUTES,
  ...COUNTS_ROUTES,
  ...MOVEMENTS_ROUTES,
];
