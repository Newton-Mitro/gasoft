import { Routes } from '@angular/router';

export const EXPENSES_ROUTES: Routes = [
  {
    path: 'expenses/create',
    loadComponent: () =>
      import('./expense-reports-page/expense-reports.page').then(
        (module) => module.ExpenseReportsPageComponent,
      ),
    data: { page: 'expenses', view: 'create' },
  },
  {
    path: 'expenses/:reportId/edit',
    loadComponent: () =>
      import('./expense-reports-page/expense-reports.page').then(
        (module) => module.ExpenseReportsPageComponent,
      ),
    data: { page: 'expenses', view: 'edit' },
  },
  {
    path: 'expenses',
    loadComponent: () =>
      import('./expense-reports-page/expense-reports.page').then(
        (module) => module.ExpenseReportsPageComponent,
      ),
    data: { page: 'expenses', view: 'index' },
  },
];
