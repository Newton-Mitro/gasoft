import { ChangeDetectorRef, Component, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { errorMessage } from '../../shared/utils/page-utils';
import { ThemeService } from '../../shared/theme/theme.service';
import { AuthenticatedImageComponent } from '../../shared/components/authenticated-image/authenticated-image.component';
import { NotificationService } from '../../shared/services/notification.service';
import { SearchableSelectDirective } from '../../shared/directives/searchable-select.directive';
import { NotificationInboxComponent } from './notification-inbox.component';

interface CompanyOption {
  id: number;
  code: string;
  legalName: string;
  functionalCurrencyCode: string;
  logoUrl: string | null;
}

interface NavLink {
  label: string;
  path: string;
  icon: string;
}

interface NavSection {
  label: string;
  links: NavLink[];
}

@Component({
  selector: 'app-shell',
  imports: [FormsModule, RouterLink, RouterLinkActive, RouterOutlet, AuthenticatedImageComponent, SearchableSelectDirective, NotificationInboxComponent],
  styleUrl: './shell.css',
  templateUrl: './shell.html',
})
export class Shell {
  readonly notifications = inject(NotificationService);

  mobileNavOpen = false;
  readonly companies = signal<CompanyOption[]>([]);
  readonly companyLoadError = signal('');
  readonly sections: NavSection[] = [
    { label: 'WORKSPACE', links: [{ label: 'Overview', path: '/dashboard', icon: 'fa-house' }] },
    {
      label: 'GENERAL LEDGER',
      links: [
        { label: 'Chart of accounts', path: '/general-ledger/accounts', icon: 'fa-book-open' },
        { label: 'Fiscal years', path: '/general-ledger/fiscal-years', icon: 'fa-calendar-days' },
        { label: 'Accounting periods', path: '/general-ledger/periods', icon: 'fa-calendar' },
        { label: 'Journals', path: '/general-ledger/journals', icon: 'fa-book' },
        { label: 'Journal entries', path: '/general-ledger/journal-entries', icon: 'fa-right-left' },
      ],
    },
    {
      label: 'CUSTOMERS & SUPPLIERS',
      links: [{ label: 'Customers & suppliers', path: '/parties', icon: 'fa-address-book' }],
    },
    {
      label: 'SALES & RECEIVABLES',
      links: [
        { label: 'Sales quotes', path: '/sales-quotes', icon: 'fa-file-signature' },
        { label: 'Sales invoices', path: '/sales-invoices', icon: 'fa-file-invoice-dollar' },
        { label: 'Credit notes', path: '/sales-credit-notes', icon: 'fa-file-circle-minus' },
      ],
    },
    {
      label: 'PURCHASING & PAYABLES',
      links: [
        { label: 'Purchase orders', path: '/purchase-orders', icon: 'fa-cart-shopping' },
        { label: 'Purchase bills', path: '/purchase-bills', icon: 'fa-receipt' },
      ],
    },
    {
      label: 'PAYMENTS',
      links: [{ label: 'Payments', path: '/payments', icon: 'fa-credit-card' }],
    },
    {
      label: 'INVENTORY',
      links: [
        { label: 'Item categories', path: '/inventory/categories', icon: 'fa-tags' },
        { label: 'Items', path: '/inventory/items', icon: 'fa-box' },
        { label: 'Warehouses', path: '/inventory/warehouses', icon: 'fa-warehouse' },
        { label: 'Stock movements', path: '/inventory/movements', icon: 'fa-arrow-right-arrow-left' },
        { label: 'Inventory counts', path: '/inventory/counts', icon: 'fa-clipboard-check' },
        { label: 'Stock balances', path: '/inventory/balances', icon: 'fa-scale-balanced' },
      ],
    },
    {
      label: 'BANKING',
      links: [{ label: 'Bank reconciliation', path: '/banking/reconciliation', icon: 'fa-building-columns' }],
    },
    {
      label: 'FIXED ASSETS',
      links: [{ label: 'Fixed assets', path: '/fixed-assets', icon: 'fa-landmark' }],
    },
    {
      label: 'REPORTS',
      links: [
        { label: 'Trial balance', path: '/reports/trial-balance', icon: 'fa-scale-balanced' },
        { label: 'Income statement', path: '/reports/income-statement', icon: 'fa-chart-line' },
        { label: 'Balance sheet', path: '/reports/balance-sheet', icon: 'fa-table-cells-large' },
        { label: 'Budgets', path: '/reports/budget-vs-actual', icon: 'fa-coins' },
        { label: 'Cash flow', path: '/reports/cash-flow', icon: 'fa-money-bill-transfer' },
        { label: 'Receivables aging', path: '/reports/receivables-aging', icon: 'fa-hourglass-half' },
        { label: 'Payables aging', path: '/reports/payables-aging', icon: 'fa-hourglass-half' },
        { label: 'Consolidated trial balance', path: '/reports/consolidation', icon: 'fa-chart-pie' },
      ],
    },
    {
      label: 'ADMINISTRATION',
      links: [
        { label: 'Company settings', path: '/settings/company', icon: 'fa-gears' },
        { label: 'Tax codes & rates', path: '/settings/tax-codes', icon: 'fa-percent' },
        { label: 'Tax registrations', path: '/settings/tax-registrations', icon: 'fa-file-circle-check' },
        { label: 'Team members', path: '/users', icon: 'fa-users' },
        { label: 'Audit history', path: '/settings/audit-history', icon: 'fa-clock-rotate-left' },
      ],
    },
  ];
  constructor(
    readonly auth: AuthService,
    readonly theme: ThemeService,
    private readonly api: ApiService,
    private readonly router: Router,
    private readonly changeDetector: ChangeDetectorRef,
  ) {
    this.api.get<CompanyOption[]>('/api/v1/tenant/companies', false).subscribe({
      next: companies => this.companies.set(companies),
      error: error => {
        this.companyLoadError.set('');
        this.notifications.error(errorMessage(error, 'Could not load your companies.'));
        this.changeDetector.markForCheck();
      },
    });
    this.auth.getProfile().subscribe({
      error: () => this.auth.profile.set(null),
    });
  }

  get currentCompany(): CompanyOption | undefined {
    return this.companies().find(company => company.id === this.auth.companyId());
  }

  get profileInitial(): string {
    return this.auth.profile()?.displayName.trim().charAt(0).toUpperCase() || 'U';
  }

  @HostListener('document:keydown.escape')
  closeMobileNavigation(): void {
    this.mobileNavOpen = false;
  }

  setThemeMode(value: string): void {
    if (value === 'light' || value === 'dark' || value === 'system') {
      this.theme.setMode(value);
    }
  }

  setThemeColor(value: string): void {
    const color = this.theme.colors.find(option => option.id === value);
    if (color) this.theme.setColor(color.id);
  }

  selectCompany(value: string | number): void {
    const companyId = Number(value);
    if (!this.companies().some(company => company.id === companyId)) return;
    this.auth.updateCompanyId(companyId);
    window.location.reload();
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }

}
