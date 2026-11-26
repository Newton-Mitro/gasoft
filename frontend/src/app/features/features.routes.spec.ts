import { BUSINESS_OPERATIONS_ROUTES } from './business-operations/business-operations.routes';
import { AUTH_ROUTES } from './auth/auth.routes';
import { DASHBOARD_ROUTES } from './dashboard/dashboard.routes';
import { GENERAL_LEDGER_ROUTES } from './general-ledger/general-ledger.routes';
import { INVENTORY_ROUTES } from './inventory/inventory.routes';
import { PROFILE_ROUTES } from './profile/profile.routes';
import { SETTINGS_ROUTES } from './settings/settings.routes';

describe('feature routes', () => {
  it('keeps auth, dashboard, and profile routes in their feature definitions', () => {
    expect(AUTH_ROUTES.map((route) => route.path)).toEqual([
      'login',
      'register',
      'forgot-password',
      'reset-password',
    ]);
    expect(DASHBOARD_ROUTES.map((route) => route.path)).toEqual(['dashboard']);
    expect(PROFILE_ROUTES.map((route) => route.path)).toEqual(['account/profile']);
  });

  it('keeps the existing report and resource routes reachable', () => {
    const routes = [
      ...GENERAL_LEDGER_ROUTES,
      ...BUSINESS_OPERATIONS_ROUTES,
      ...INVENTORY_ROUTES,
      ...SETTINGS_ROUTES,
    ];
    const routedPages = new Set(
      routes
        .map((route) => route.data?.['page'])
        .filter((page): page is string => typeof page === 'string'),
    );

    expect(routedPages.has('fiscal-years')).toBe(true);
    expect(routedPages.has('periods')).toBe(true);
    expect(routedPages.has('trial-balance')).toBe(true);
    expect(routedPages.has('income-statement')).toBe(true);
    expect(routedPages.has('balance-sheet')).toBe(true);
    expect(routedPages.has('budget-vs-actual')).toBe(true);
    expect(routedPages.has('fixed-assets')).toBe(true);
    expect(routedPages.has('pos')).toBe(true);
    expect(routedPages.has('notification-settings')).toBe(true);
    expect(routedPages.has('email-configuration')).toBe(true);
    expect(SETTINGS_ROUTES.find((route) => route.path === 'settings/pos')?.redirectTo).toBe('/pos');
  });

  it('routes resource pages through distinct feature components', () => {
    const routes = [
      ...GENERAL_LEDGER_ROUTES,
      ...BUSINESS_OPERATIONS_ROUTES,
      ...INVENTORY_ROUTES,
      ...SETTINGS_ROUTES,
    ];
    const componentByPage = new Map<string, unknown>();
    for (const route of routes) {
      const page = route.data?.['page'];
      if (typeof page === 'string' && !componentByPage.has(page)) {
        componentByPage.set(page, route.component ?? route.loadComponent);
      }
    }

    expect(componentByPage.get('accounts')).not.toBe(componentByPage.get('account-groups'));
    expect(componentByPage.get('parties')).not.toBe(componentByPage.get('sales-invoices'));
    expect(componentByPage.get('parties')).not.toBe(componentByPage.get('party-addresses'));
    expect(componentByPage.get('party-addresses')).not.toBe(componentByPage.get('parties'));
    expect(componentByPage.get('items')).not.toBe(componentByPage.get('warehouses'));
    expect(componentByPage.get('item-categories')).not.toBe(componentByPage.get('items'));
    expect(componentByPage.get('warehouses')).not.toBe(componentByPage.get('items'));
    expect(componentByPage.get('inventory-balances')).not.toBe(componentByPage.get('warehouses'));
    expect(componentByPage.get('inventory-counts')).not.toBe(
      componentByPage.get('inventory-balances'),
    );
    expect(componentByPage.get('inventory-movements')).not.toBe(
      componentByPage.get('inventory-counts'),
    );
    expect(componentByPage.get('accounts')).toBeDefined();
    expect(componentByPage.get('parties')).toBeDefined();
    expect(componentByPage.get('sales-invoices')).not.toBe(componentByPage.get('parties'));
    expect(componentByPage.get('sales-invoices')).toBeDefined();
    expect(componentByPage.get('journal-entries')).toBeDefined();
    expect(componentByPage.get('journal-entries')).not.toBe(componentByPage.get('journals'));
    expect(componentByPage.get('purchase-bills')).not.toBe(componentByPage.get('sales-invoices'));
    expect(componentByPage.get('purchase-bills')).toBeDefined();
    expect(componentByPage.get('payments')).not.toBe(componentByPage.get('purchase-bills'));
    expect(componentByPage.get('payments')).toBeDefined();
    expect(componentByPage.get('tax-registrations')).not.toBe(componentByPage.get('users'));
    expect(componentByPage.get('tax-registrations')).toBeDefined();
    expect(componentByPage.get('users')).toBeDefined();
    expect(componentByPage.get('company')).toBeDefined();
    expect(componentByPage.get('party-addresses')).toBeDefined();
    expect(componentByPage.get('account-groups')).toBeDefined();
    expect(componentByPage.get('fiscal-years')).toBeDefined();
    expect(componentByPage.get('periods')).toBeDefined();
    expect(componentByPage.get('journals')).toBeDefined();
    expect(componentByPage.get('item-categories')).toBeDefined();
    expect(componentByPage.get('warehouses')).toBeDefined();
    expect(componentByPage.get('inventory-balances')).toBeDefined();
    expect(componentByPage.get('inventory-counts')).toBeDefined();
    expect(componentByPage.get('inventory-movements')).toBeDefined();
    expect(componentByPage.get('trial-balance')).not.toBe(componentByPage.get('income-statement'));
    expect(componentByPage.get('income-statement')).not.toBe(componentByPage.get('balance-sheet'));
    expect(componentByPage.get('budget-vs-actual')).toBeDefined();
    expect(componentByPage.get('fixed-assets')).toBeDefined();
  });
});
