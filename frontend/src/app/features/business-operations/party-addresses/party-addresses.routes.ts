import { Routes } from '@angular/router';

export const PARTY_ADDRESSES_ROUTES: Routes = [
  { path: 'parties/:partyId/addresses/create', loadComponent: () => import('./party-addresses-page/party-addresses.page').then(module => module.PartyAddressesPageComponent), data: { page: 'party-addresses', view: 'create' } },
  { path: 'parties/:partyId/addresses/:recordId/edit', loadComponent: () => import('./party-addresses-page/party-addresses.page').then(module => module.PartyAddressesPageComponent), data: { page: 'party-addresses', view: 'edit' } },
  { path: 'parties/:partyId/addresses', loadComponent: () => import('./party-addresses-page/party-addresses.page').then(module => module.PartyAddressesPageComponent), data: { page: 'party-addresses' } },
];
