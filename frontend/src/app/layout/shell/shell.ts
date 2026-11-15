import { ChangeDetectorRef, Component, HostListener, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { errorMessage } from '../../shared/utils/page-utils';
import { ThemeService } from '../../shared/theme/theme.service';
import { AuthenticatedImageComponent } from '../../shared/components/authenticated-image/authenticated-image.component';

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
  imports: [FormsModule, RouterLink, RouterLinkActive, RouterOutlet, AuthenticatedImageComponent],
  styleUrl: './shell.css',
  templateUrl: './shell.html',
})
export class Shell {
  mobileNavOpen = false;
  readonly companies = signal<CompanyOption[]>([]);
  readonly companyLoadError = signal('');
  readonly sections: NavSection[] = [
    { label: 'WORKSPACE', links: [{ label: 'Overview', path: '/dashboard', icon: '⌂' }] },
    {
      label: 'GENERAL LEDGER',
      links: [
        { label: 'Chart of accounts', path: '/general-ledger/accounts', icon: '▤' },
        { label: 'Account groups', path: '/general-ledger/account-groups', icon: '▦' },
        { label: 'Fiscal years', path: '/general-ledger/fiscal-years', icon: '◷' },
        { label: 'Accounting periods', path: '/general-ledger/periods', icon: '▥' },
        { label: 'Journals', path: '/general-ledger/journals', icon: '≡' },
        { label: 'Journal entries', path: '/general-ledger/journal-entries', icon: '↔' },
      ],
    },
    {
      label: 'OPERATIONS',
      links: [
        { label: 'Customers & suppliers', path: '/parties', icon: '♧' },
        { label: 'Sales invoices', path: '/sales-invoices', icon: '↗' },
        { label: 'Purchase bills', path: '/purchase-bills', icon: '↙' },
        { label: 'Payments', path: '/payments', icon: '◉' },
        { label: 'Items', path: '/inventory/items', icon: '◇' },
        { label: 'Item categories', path: '/inventory/categories', icon: '▧' },
        { label: 'Warehouses', path: '/inventory/warehouses', icon: '⌂' },
        { label: 'Stock balances', path: '/inventory/balances', icon: '▥' },
        { label: 'Stock movements', path: '/inventory/movements', icon: '↔' },
        { label: 'Inventory counts', path: '/inventory/counts', icon: '☷' },
      ],
    },
    {
      label: 'REPORTS',
      links: [
        { label: 'Trial balance', path: '/reports/trial-balance', icon: '◫' },
        { label: 'Income statement', path: '/reports/income-statement', icon: '⌁' },
        { label: 'Balance sheet', path: '/reports/balance-sheet', icon: '▦' },
      ],
    },
    {
      label: 'ADMINISTRATION',
      links: [
        { label: 'Company settings', path: '/settings/company', icon: '⚙' },
        { label: 'Tax registrations', path: '/settings/tax-registrations', icon: '%' },
        { label: 'Team members', path: '/users', icon: '♙' },
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
        this.companyLoadError.set(errorMessage(error, 'Could not load your companies.'));
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
