import { Routes } from '@angular/router';

export const SALES_QUOTES_ROUTES: Routes = [
  { path: 'sales-quotes', loadComponent: () => import('./sales-quotes-page/sales-quotes.page').then(module => module.SalesQuotesPageComponent), data: { page: 'sales-quotes' } },
];
