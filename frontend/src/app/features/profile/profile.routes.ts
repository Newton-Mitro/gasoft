import { Routes } from '@angular/router';

export const PROFILE_ROUTES: Routes = [
  { path: 'account/profile', loadComponent: () => import('./account-profile-page/account-profile.page').then(module => module.AccountProfilePage) },
];
