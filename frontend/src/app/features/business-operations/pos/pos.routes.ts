import { Routes } from '@angular/router';

export const POS_ROUTES: Routes = [
  {
    path: 'pos',
    loadComponent: () => import('./pos-page/pos.page').then((module) => module.PosPageComponent),
    data: { page: 'pos' },
  },
];
