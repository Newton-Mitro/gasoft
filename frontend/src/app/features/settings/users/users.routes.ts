import { Routes } from '@angular/router';

export const USERS_ROUTES: Routes = [
  { path: 'users/create', loadComponent: () => import('./users-page/users.page').then(module => module.UsersPageComponent), data: { page: 'users', view: 'create' } },
  { path: 'users/:recordId/edit', loadComponent: () => import('./users-page/users.page').then(module => module.UsersPageComponent), data: { page: 'users', view: 'edit' } },
  { path: 'users', loadComponent: () => import('./users-page/users.page').then(module => module.UsersPageComponent), data: { page: 'users' } },
];
