import { Routes } from '@angular/router';
import { ACCOUNTS_ROUTES } from './accounts/accounts.routes';
import { ACCOUNT_GROUPS_ROUTES } from './account-groups/account-groups.routes';
import { FISCAL_YEARS_ROUTES } from './fiscal-years/fiscal-years.routes';
import { PERIODS_ROUTES } from './periods/periods.routes';
import { JOURNALS_ROUTES } from './journals/journals.routes';
import { JOURNAL_ENTRIES_ROUTES } from './journal-entries/journal-entries.routes';
import { REPORTS_ROUTES } from '../reports/reports.routes';

export const GENERAL_LEDGER_ROUTES: Routes = [
  ...ACCOUNTS_ROUTES,
  ...ACCOUNT_GROUPS_ROUTES,
  ...FISCAL_YEARS_ROUTES,
  ...PERIODS_ROUTES,
  ...JOURNALS_ROUTES,
  ...JOURNAL_ENTRIES_ROUTES,
  ...REPORTS_ROUTES,
];
