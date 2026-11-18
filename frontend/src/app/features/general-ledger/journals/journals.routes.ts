import { Routes } from '@angular/router';

export const JOURNALS_ROUTES: Routes = [
  { path: 'general-ledger/journals/create', loadComponent: () => import('./journals-page/journals.page').then(module => module.JournalsPageComponent), data: { page: 'journals', view: 'create' } },
  { path: 'general-ledger/journals/:recordId/edit', loadComponent: () => import('./journals-page/journals.page').then(module => module.JournalsPageComponent), data: { page: 'journals', view: 'edit' } },
  { path: 'general-ledger/journals', loadComponent: () => import('./journals-page/journals.page').then(module => module.JournalsPageComponent), data: { page: 'journals' } },
];
