import { Routes } from '@angular/router';

export const ACCOUNT_GROUPS_ROUTES: Routes = [
  { path: 'general-ledger/account-groups/create', loadComponent: () => import('./account-groups-page/account-groups.page').then(module => module.AccountGroupsPageComponent), data: { page: 'account-groups', view: 'create' } },
  { path: 'general-ledger/account-groups/:recordId/edit', loadComponent: () => import('./account-groups-page/account-groups.page').then(module => module.AccountGroupsPageComponent), data: { page: 'account-groups', view: 'edit' } },
  { path: 'general-ledger/account-groups', loadComponent: () => import('./account-groups-page/account-groups.page').then(module => module.AccountGroupsPageComponent), data: { page: 'account-groups' } },
];
