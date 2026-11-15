import { Routes } from '@angular/router';
export const BUSINESS_ROUTES: Routes = [
  { path: 'parties/create', loadComponent: () => import('./parties/parties.page').then(module => module.PartiesPageComponent), data: { page: 'parties', view: 'create' } },
  { path: 'parties/:recordId/edit', loadComponent: () => import('./parties/parties.page').then(module => module.PartiesPageComponent), data: { page: 'parties', view: 'edit' } },
  { path: 'parties', loadComponent: () => import('./parties/parties.page').then(module => module.PartiesPageComponent), data: { page: 'parties' } },
  { path: 'parties/:partyId/addresses/create', loadComponent: () => import('./party-addresses/party-addresses.page').then(module => module.PartyAddressesPageComponent), data: { page: 'party-addresses', view: 'create' } },
  { path: 'parties/:partyId/addresses/:recordId/edit', loadComponent: () => import('./party-addresses/party-addresses.page').then(module => module.PartyAddressesPageComponent), data: { page: 'party-addresses', view: 'edit' } },
  { path: 'parties/:partyId/addresses', loadComponent: () => import('./party-addresses/party-addresses.page').then(module => module.PartyAddressesPageComponent), data: { page: 'party-addresses' } },
  { path: 'sales-invoices/create', loadComponent: () => import('./sales-invoices/sales-invoices.page').then(module => module.SalesInvoicesPageComponent), data: { page: 'sales-invoices', view: 'create' } },
  { path: 'sales-invoices/:recordId/edit', loadComponent: () => import('./sales-invoices/sales-invoices.page').then(module => module.SalesInvoicesPageComponent), data: { page: 'sales-invoices', view: 'edit' } },
  { path: 'sales-invoices', loadComponent: () => import('./sales-invoices/sales-invoices.page').then(module => module.SalesInvoicesPageComponent), data: { page: 'sales-invoices' } },
  { path: 'purchase-bills/create', loadComponent: () => import('./purchase-bills/purchase-bills.page').then(module => module.PurchaseBillsPageComponent), data: { page: 'purchase-bills', view: 'create' } },
  { path: 'purchase-bills/:recordId/edit', loadComponent: () => import('./purchase-bills/purchase-bills.page').then(module => module.PurchaseBillsPageComponent), data: { page: 'purchase-bills', view: 'edit' } },
  { path: 'purchase-bills', loadComponent: () => import('./purchase-bills/purchase-bills.page').then(module => module.PurchaseBillsPageComponent), data: { page: 'purchase-bills' } },
  { path: 'payments/create', loadComponent: () => import('./payments/payments.page').then(module => module.PaymentsPageComponent), data: { page: 'payments', view: 'create' } },
  { path: 'payments/:recordId/edit', loadComponent: () => import('./payments/payments.page').then(module => module.PaymentsPageComponent), data: { page: 'payments', view: 'edit' } },
  { path: 'payments', loadComponent: () => import('./payments/payments.page').then(module => module.PaymentsPageComponent), data: { page: 'payments' } },
];
