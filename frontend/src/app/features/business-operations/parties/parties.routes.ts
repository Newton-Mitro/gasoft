import { Routes } from '@angular/router';

export const PARTIES_ROUTES: Routes = [
  { path: 'parties/create', loadComponent: () => import('./parties-page/parties.page').then(module => module.PartiesPageComponent), data: { page: 'parties', view: 'create' } },
  { path: 'parties/:recordId/edit', loadComponent: () => import('./parties-page/parties.page').then(module => module.PartiesPageComponent), data: { page: 'parties', view: 'edit' } },
  { path: 'parties', loadComponent: () => import('./parties-page/parties.page').then(module => module.PartiesPageComponent), data: { page: 'parties' } },
];
