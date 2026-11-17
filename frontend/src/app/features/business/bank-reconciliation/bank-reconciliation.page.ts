import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PaginationComponent, PageChange } from '../../../shared/components/pagination/pagination.component';
import { errorMessage } from '../../../shared/utils/page-utils';
import { Choice } from '../../../shared/models/shared.models';
import { OptionDataCacheService } from '../../../shared/data/option-data-cache.service';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { AuthService } from '../../../core/auth.service';
import { CompanyService } from '../../settings/company/company.service';
import { Company } from '../../settings/company/company.model';
import {
  BankAccount,
  BankMatchCandidate,
  BankTransaction,
} from './bank-reconciliation.model';
import { BankReconciliationService } from './bank-reconciliation.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { SearchableSelectDirective } from '../../../shared/directives/searchable-select.directive';

@Component({
  selector: 'app-bank-reconciliation-page',
  imports: [FormsModule, PaginationComponent, FieldValidationDirective, SearchableSelectDirective],
  templateUrl: './bank-reconciliation.page.html',
  styleUrls: ['../../../shared/styles/feature-page.css', './bank-reconciliation.page.css'],
})
export class BankReconciliationPageComponent implements OnInit {
  readonly notifications = inject(NotificationService);
  accounts: BankAccount[] = [];
  transactions: BankTransaction[] = [];
  page = 1;
  pageSize = 25;
  totalCount = 0;
  totalPages = 0;
  chartAccounts: Choice[] = [];
  adjustmentAccounts: Choice[] = [];
  company: Company | null = null;
  selectedAccountId: number | null = null;
  name = '';
  bankName = '';
  accountNumberLastFour = '';
  chartOfAccountId: number | null = null;
  selectedFile: File | null = null;
  showAccountForm = false;
  matchTransactionId: number | null = null;
  adjustmentTransactionId: number | null = null;
  adjustmentAccountId: number | null = null;
  adjustmentEntryDate = '';
  adjustmentDescription = '';
  allocationAmounts: Record<number, number> = {};
  candidates: BankMatchCandidate[] = [];
  loading = false;
  saving = false;
  importing = false;
  error = '';
  importMessage = '';
  matchError = '';

  constructor(
    private readonly bankService: BankReconciliationService,
    private readonly companyService: CompanyService,
    private readonly auth: AuthService,
    private readonly optionData: OptionDataService,
    private readonly optionCache: OptionDataCacheService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadAccounts();
    this.loadChartAccounts();
    this.loadAdjustmentAccounts();
    const companyId = this.auth.companyId();
    if (!companyId) {
      this.error = 'Choose a company before managing bank accounts.';
      return;
    }
    this.companyService.getById(companyId).subscribe({
      next: company => {
        this.company = company;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load company currency.'));
        this.changeDetector.markForCheck();
      },
    });
  }

  get selectedAccount(): BankAccount | undefined {
    return this.accounts.find(account => account.id === this.selectedAccountId);
  }

  get matchedCount(): number {
    return this.transactions.filter(transaction => transaction.remainingAmount === 0).length;
  }

  get partialCount(): number {
    return this.transactions.filter(transaction =>
      transaction.matchedAmount !== 0 && transaction.remainingAmount !== 0).length;
  }

  get unmatchedCount(): number {
    return this.transactions.filter(transaction => transaction.matchedAmount === 0).length;
  }

  onPageChange(change: PageChange): void {
    this.page = change.page;
    this.pageSize = change.pageSize;
    this.loadTransactions();
  }

  createAccount(): void {
    if (!this.company || !this.name.trim() || !this.bankName.trim() ||
        !/^[A-Za-z0-9]{4}$/.test(this.accountNumberLastFour) ||
        !this.chartOfAccountId) {
      this.error = 'Enter account and bank names, the last four account digits, and a chart-of-accounts asset.';
      return;
    }
    this.error = '';
    this.saving = true;
    this.bankService.createAccount({
      name: this.name.trim(),
      bankName: this.bankName.trim(),
      accountNumberLastFour: this.accountNumberLastFour.trim().toUpperCase(),
      currencyCode: this.company.functionalCurrencyCode,
      chartOfAccountId: this.chartOfAccountId,
    }).subscribe({
      next: account => {
        this.accounts = [...this.accounts, account];
        this.selectedAccountId = account.id;
        this.page = 1;
        this.showAccountForm = false;
        this.name = '';
        this.bankName = '';
        this.accountNumberLastFour = '';
        this.chartOfAccountId = null;
        this.saving = false;
        this.notifications.success('Bank account created successfully.');
        this.loadTransactions();
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not create this bank account.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  selectAccount(value: number | string): void {
    const accountId = Number(value);
    if (!this.accounts.some(account => account.id === accountId)) return;
    this.selectedAccountId = accountId;
    this.page = 1;
    this.transactions = [];
    this.matchTransactionId = null;
    this.importMessage = '';
    this.loadTransactions();
  }

  openAccountForm(): void {
    this.showAccountForm = true;
    this.error = '';
  }

  onStatementSelected(event: Event): void {
    const input = event.currentTarget as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
    this.importMessage = '';
    this.error = '';
    if (!this.selectedFile) return;
    if (!this.selectedFile.name.toLowerCase().endsWith('.csv') || this.selectedFile.size > 10 * 1024 * 1024) {
      this.error = 'Choose a CSV statement no larger than 10 MB.';
      this.selectedFile = null;
      input.value = '';
    }
  }

  importStatement(): void {
    if (!this.selectedAccountId || !this.selectedFile) return;
    this.importing = true;
    this.error = '';
    this.importMessage = '';
    this.bankService.importStatement(this.selectedAccountId, this.selectedFile).subscribe({
      next: result => {
        this.importing = false;
        this.importMessage = `Imported ${result.imported} transaction${result.imported === 1 ? '' : 's'}; skipped ${result.duplicates} duplicate${result.duplicates === 1 ? '' : 's'}.`;
        this.notifications.success(this.importMessage);
        this.selectedFile = null;
        this.page = 1;
        this.optionCache.clear();
        this.loadTransactions();
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not import the bank statement.'));
        this.importing = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  showMatches(transaction: BankTransaction): void {
    const accountId = this.selectedAccountId;
    if (!accountId) return;
    this.matchError = '';
    this.candidates = [];
    this.allocationAmounts = {};
    this.matchTransactionId = transaction.id;
    this.bankService.getCandidates(accountId, transaction.id).subscribe({
      next: candidates => {
        this.candidates = candidates;
        this.allocationAmounts = Object.fromEntries(candidates.map(candidate => [
          candidate.journalLineId,
          Math.min(Math.abs(transaction.remainingAmount), Math.abs(candidate.amount)),
        ]));
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.matchError = '';
        this.notifications.error(errorMessage(error, 'Could not load matching ledger entries.'));
        this.changeDetector.markForCheck();
      },
    });
  }

  matchTransaction(transaction: BankTransaction, candidate: BankMatchCandidate): void {
    const accountId = this.selectedAccountId;
    if (!accountId) return;
    const allocation = Number(this.allocationAmounts[candidate.journalLineId]);
    if (!Number.isFinite(allocation) || allocation <= 0 ||
      allocation > Math.min(Math.abs(transaction.remainingAmount), Math.abs(candidate.amount))) {
      this.matchError = 'Enter a positive allocation no greater than the statement or ledger remaining amount.';
      return;
    }
    this.saving = true;
    this.matchError = '';
    this.bankService.allocate(
      accountId,
      transaction.id,
      candidate.journalLineId,
      Math.sign(transaction.remainingAmount) * allocation,
    ).subscribe({
      next: () => {
        this.saving = false;
        this.notifications.success('Bank transaction matched successfully.');
        this.loadTransactions(transaction.id);
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.matchError = '';
        this.notifications.error(errorMessage(error, 'Could not match this transaction.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  unmatchTransaction(transaction: BankTransaction, matchId: number): void {
    const accountId = this.selectedAccountId;
    if (!accountId) return;
    this.saving = true;
    this.matchError = '';
    this.bankService.unmatch(accountId, transaction.id, matchId).subscribe({
      next: () => {
        this.saving = false;
        this.notifications.success('Bank transaction match removed.');
        this.loadTransactions(transaction.id);
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.matchError = '';
        this.notifications.error(errorMessage(error, 'Could not remove this match.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  openAdjustment(transaction: BankTransaction): void {
    this.adjustmentTransactionId = transaction.id;
    this.adjustmentAccountId = null;
    this.adjustmentEntryDate = transaction.transactionDate;
    this.adjustmentDescription = `Bank adjustment: ${transaction.description}`;
    this.matchError = '';
  }

  recordAdjustment(transaction: BankTransaction): void {
    const accountId = this.selectedAccountId;
    if (!accountId || !this.adjustmentAccountId || !this.adjustmentEntryDate) {
      this.matchError = 'Choose an offset account and an entry date.';
      return;
    }
    this.saving = true;
    this.matchError = '';
    this.bankService.createAdjustment(accountId, transaction.id, {
      offsetAccountId: this.adjustmentAccountId,
      entryDate: this.adjustmentEntryDate,
      description: this.adjustmentDescription.trim() || null,
    }).subscribe({
      next: () => {
        this.saving = false;
        this.adjustmentTransactionId = null;
        this.importMessage = 'A posted journal adjustment was created and matched to the statement transaction.';
        this.notifications.success(this.importMessage);
        this.loadTransactions(transaction.id);
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.matchError = '';
        this.notifications.error(errorMessage(error, 'Could not create the reconciliation adjustment.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  transactionSign(amount: number): string {
    return amount > 0 ? 'deposit' : 'withdrawal';
  }

  formatAmount(amount: number): string {
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: this.selectedAccount?.currencyCode ?? this.company?.functionalCurrencyCode ?? 'USD',
    }).format(amount);
  }

  absoluteAmount(amount: number): number {
    return Math.abs(amount);
  }

  private loadAccounts(): void {
    this.loading = true;
    this.bankService.getAccounts().subscribe({
      next: accounts => {
        this.accounts = accounts;
        if (!this.selectedAccountId && accounts.length) this.selectedAccountId = accounts[0].id;
        this.loading = false;
        if (this.selectedAccountId) this.loadTransactions();
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load bank accounts.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadChartAccounts(): void {
    this.optionData.getBankAssetAccountOptions().subscribe({
      next: accounts => {
        this.chartAccounts = accounts;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load asset accounts.'));
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadAdjustmentAccounts(): void {
    this.optionData.getOptions('accounts').subscribe({
      next: accounts => {
        this.adjustmentAccounts = accounts;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load offset accounts.'));
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadTransactions(expandedTransactionId?: number): void {
    if (!this.selectedAccountId) return;
    this.loading = true;
    this.bankService.getTransactionsPage(this.selectedAccountId, this.page, this.pageSize).subscribe({
      next: result => {
        this.transactions = result.items;
        this.page = result.page;
        this.pageSize = result.pageSize;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.loading = false;
        if (expandedTransactionId) {
          const transaction = result.items.find(item => item.id === expandedTransactionId);
          if (transaction && transaction.remainingAmount !== 0) this.showMatches(transaction);
          else {
            this.matchTransactionId = null;
            this.adjustmentTransactionId = null;
          }
        }
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load bank statement transactions.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}
