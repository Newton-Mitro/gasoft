import { Routes } from '@angular/router';
import { TAX_JURISDICTIONS_ROUTES } from './tax-jurisdictions/tax-jurisdictions.routes';
import { TAX_CODES_ROUTES } from './tax-codes/tax-codes.routes';
import { TAX_REGISTRATIONS_ROUTES } from './tax-registrations/tax-registrations.routes';
import { COMPANY_ROUTES } from './company/company.routes';
import { AUDIT_HISTORY_ROUTES } from './audit-history/audit-history.routes';
import { USERS_ROUTES } from './users/users.routes';

export const SETTINGS_ROUTES: Routes = [
  {
    path: 'settings/pos',
    loadComponent: () =>
      import('../business-operations/pos/pos-settings-page/pos-settings.page').then(
        (module) => module.PosSettingsPageComponent,
      ),
    data: { page: 'pos-settings' },
  },
  ...TAX_JURISDICTIONS_ROUTES,
  ...TAX_CODES_ROUTES,
  ...TAX_REGISTRATIONS_ROUTES,
  ...COMPANY_ROUTES,
  ...AUDIT_HISTORY_ROUTES,
  ...USERS_ROUTES,
];
