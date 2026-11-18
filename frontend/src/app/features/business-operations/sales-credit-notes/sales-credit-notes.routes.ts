import { Routes } from '@angular/router';

export const SALES_CREDIT_NOTES_ROUTES: Routes = [
  { path: 'sales-credit-notes', loadComponent: () => import('./sales-credit-notes-page/sales-credit-notes.page').then(module => module.SalesCreditNotesPageComponent), data: { page: 'sales-credit-notes' } },
];
