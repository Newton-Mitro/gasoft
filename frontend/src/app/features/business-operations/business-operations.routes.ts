import { Routes } from '@angular/router';
import { FIXED_ASSETS_ROUTES } from './fixed-assets/fixed-assets.routes';
import { PURCHASE_ORDERS_ROUTES } from './purchase-orders/purchase-orders.routes';
import { BANK_RECONCILIATION_ROUTES } from './bank-reconciliation/bank-reconciliation.routes';
import { PARTIES_ROUTES } from './parties/parties.routes';
import { PARTY_ADDRESSES_ROUTES } from './party-addresses/party-addresses.routes';
import { SALES_QUOTES_ROUTES } from './sales-quotes/sales-quotes.routes';
import { SALES_CREDIT_NOTES_ROUTES } from './sales-credit-notes/sales-credit-notes.routes';
import { SALES_INVOICES_ROUTES } from './sales-invoices/sales-invoices.routes';
import { PURCHASE_BILLS_ROUTES } from './purchase-bills/purchase-bills.routes';
import { PAYMENTS_ROUTES } from './payments/payments.routes';
import { POS_ROUTES } from './pos/pos.routes';
import { EXPENSES_ROUTES } from './expenses/expenses.routes';

export const BUSINESS_OPERATIONS_ROUTES: Routes = [
  ...FIXED_ASSETS_ROUTES,
  ...PURCHASE_ORDERS_ROUTES,
  ...BANK_RECONCILIATION_ROUTES,
  ...PARTIES_ROUTES,
  ...PARTY_ADDRESSES_ROUTES,
  ...SALES_QUOTES_ROUTES,
  ...POS_ROUTES,
  ...SALES_CREDIT_NOTES_ROUTES,
  ...SALES_INVOICES_ROUTES,
  ...PURCHASE_BILLS_ROUTES,
  ...PAYMENTS_ROUTES,
  ...EXPENSES_ROUTES,
];
