import { Routes } from '@angular/router';

export const FIXED_ASSETS_ROUTES: Routes = [
  { path: 'fixed-assets', loadComponent: () => import('./fixed-assets-page/fixed-assets.page').then(module => module.FixedAssetsPageComponent), data: { page: 'fixed-assets' } },
];
