import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { PaginationComponent, PageChange } from '../../../../shared/components/pagination/pagination.component';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { Choice, Field } from '../../../../shared/models/shared.models';
import { RecordPreviewComponent, shouldShowRecordPreview } from '../../../../shared/components/record-preview/record-preview.component';
import { OptionDataCacheService } from '../../../../shared/data/option-data-cache.service';
import { OptionDataService } from '../../../../shared/data/option-data.service';
import { AuthService } from '../../../../core/auth.service';
import { CompanyService } from '../../../settings/company/company.service';
import { Company } from '../../../settings/company/company.model';
import {
  BankAccount,
  BankMatchCandidate,
  BankTransaction,
} from '../bank-reconciliation.model';
import { BankReconciliationService } from '../bank-reconciliation.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { FieldValidationDirective } from '../../../../shared/directives/field-validation.directive';
import { SearchableSelectDirective } from '../../../../shared/directives/searchable-select.directive';
import { InputComponent } from '../../../../shared/components/input/input.component';
import {
  FormValidationErrors,
  FormValidationService,
} from '../../../../shared/services/form-validation.service';

@Component({
  selector: 'app-bank-reconciliation-page',
  imports: [FormsModule, PaginationComponent, RecordPreviewComponent, FieldValidationDirective, SearchableSelectDirective, InputComponent],
  templateUrl: './bank-reconciliation.page.html',
  styleUrls: ['../../../../shared/styles/feature-page.css', './bank-reconciliation.page.css'],
})
export class BankReconciliationPageComponent implements OnInit {
  readonly notifications = inject(NotificationService);
  private readonly formValidation = inject(FormValidationService);
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
  statementFileError = '';
  showAccountForm = false;
  matchTransactionId: number | null = null;
  adjustmentTransactionId: number | null = null;
  adjustmentAccountId: number | null = null;
  adjustmentEntryDate = '';
  adjustmentDescription = '';
  allocationAmounts: Record<number, number> = {};
  candidates: BankMatchCandidate[] = [];
  readonly bankAccountPreviewFields: Field[] = [
    { key: 'name', label: 'Account name' },
    { key: 'bankName', label: 'Bank name' },
    { key: 'lastFour', label: 'Last four digits' },
    { key: 'chartOfAccountId', label: 'Ledger asset account', type: 'select', optionSource: 'accounts' },
    { key: 'currencyCode', label: 'Functional currency' },
  ];
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

  get showBankAccountPreview(): boolean {
    return shouldShowRecordPreview('bank-accounts', 'create', this.bankAccountPreviewFields);
  }

  get bankAccountPreviewForm(): Record<string, unknown> {
    return {
      name: this.name,
      bankName: this.bankName,
      lastFour: this.accountNumberLastFour,
      chartAccountId: this.chartOfAccountId,
      currencyCode: this.company?.functionalCurrencyCode ?? '',
    };
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
    this.formValidation.clear();
    const validationErrors = validateBankAccount(
      this.name,
      this.bankName,
      this.accountNumberLastFour,
      this.chartOfAccountId,
    );
    if (Object.keys(validationErrors).length) {
      this.formValidation.setErrors(validationErrors);
      return;
    }
    if (!this.company) {
      this.error = 'Choose a company before managing bank accounts.';
      return;
    }
    const chartOfAccountId = this.chartOfAccountId;
    if (chartOfAccountId === null) return;
    this.error = '';
    this.saving = true;
    this.bankService.createAccount({
      name: this.name.trim(),
      bankName: this.bankName.trim(),
      accountNumberLastFour: this.accountNumberLastFour.trim().toUpperCase(),
      currencyCode: this.company.functionalCurrencyCode,
      chartOfAccountId,
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
        this.formValidation.clear();
        this.notifications.success('Bank account created successfully.');
        this.loadTransactions();
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.showServerValidationErrors(error, 'Could not create this bank account.');
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
    this.formValidation.clear();
    this.showAccountForm = true;
    this.error = '';
  }

  closeAccountForm(): void {
    this.formValidation.clear();
    this.showAccountForm = false;
  }

  onStatementSelected(event: Event): void {
    const input = event.currentTarget as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
    this.importMessage = '';
    this.error = '';
    this.statementFileError = '';
    if (!this.selectedFile) return;
    if (!this.selectedFile.name.toLowerCase().endsWith('.csv') || this.selectedFile.size > 10 * 1024 * 1024) {
      this.statementFileError = 'Choose a CSV statement no larger than 10 MB.';
      this.selectedFile = null;
      input.value = '';
    }
  }

  importStatement(): void {
    if (!this.selectedAccountId || !this.selectedFile) return;
    this.importing = true;
    this.error = '';
    this.importMessage = '';
    this.statementFileError = '';
    this.bankService.importStatement(this.selectedAccountId, this.selectedFile).subscribe({
      next: result => {
        this.importing = false;
        this.importMessage = `Imported ${result.imported} transaction${result.imported === 1 ? '' : 's'}; skipped ${result.duplicates} duplicate${result.duplicates === 1 ? '' : 's'}.`;
        this.notifications.success(this.importMessage);
        this.selectedFile = null;
        this.statementFileError = '';
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
    this.formValidation.clear();
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
    const field = `matchAmount-${candidate.journalLineId}`;
    const allocation = this.allocationAmounts[candidate.journalLineId];
    const validationErrors = validateAllocation(
      allocation,
      Math.min(Math.abs(transaction.remainingAmount), Math.abs(candidate.amount)),
    );
    if (validationErrors.length) {
      this.formValidation.setErrors({
        ...this.formValidation.errors(),
        [field]: validationErrors,
      });
      return;
    }
    this.saving = true;
    this.formValidation.clearField(field);
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
    this.formValidation.clear();
    this.adjustmentTransactionId = transaction.id;
    this.adjustmentAccountId = null;
    this.adjustmentEntryDate = transaction.transactionDate;
    this.adjustmentDescription = `Bank adjustment: ${transaction.description}`;
    this.matchError = '';
  }

  closeAdjustmentForm(): void {
    this.formValidation.clear();
    this.adjustmentTransactionId = null;
  }

  recordAdjustment(transaction: BankTransaction): void {
    const accountId = this.selectedAccountId;
    const offsetAccountId = this.adjustmentAccountId;
    this.formValidation.clear();
    const validationErrors = validateAdjustment(
      offsetAccountId,
      this.adjustmentEntryDate,
      this.adjustmentDescription,
    );
    if (Object.keys(validationErrors).length) {
      this.formValidation.setErrors(validationErrors);
      return;
    }
    if (!accountId || !offsetAccountId) return;
    this.saving = true;
    this.matchError = '';
    this.bankService.createAdjustment(accountId, transaction.id, {
      offsetAccountId,
      entryDate: this.adjustmentEntryDate,
      description: this.adjustmentDescription.trim() || null,
    }).subscribe({
      next: () => {
        this.saving = false;
        this.adjustmentTransactionId = null;
        this.formValidation.clear();
        this.importMessage = 'A posted journal adjustment was created and matched to the statement transaction.';
        this.notifications.success(this.importMessage);
        this.loadTransactions(transaction.id);
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.matchError = '';
        this.showServerValidationErrors(error, 'Could not create the reconciliation adjustment.');
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

  private showServerValidationErrors(error: unknown, fallback: string): void {
    const validationErrors = serverValidationErrors(error);
    if (!Object.keys(validationErrors).length) {
      this.notifications.error(errorMessage(error, fallback));
      return;
    }

    this.formValidation.setErrors(validationErrors);
    if (Object.keys(this.formValidation.unmatchedErrors()).length) {
      this.notifications.error(errorMessage(error, fallback));
    }
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

function validateBankAccount(
  name: string,
  bankName: string,
  accountNumberLastFour: string,
  chartOfAccountId: number | null,
): FormValidationErrors {
  const errors: FormValidationErrors = {};
  if (!name.trim()) errors['name'] = ['Enter an account name.'];
  else if (name.trim().length > 120) errors['name'] = ['Account name must be 120 characters or fewer.'];
  if (!bankName.trim()) errors['bankName'] = ['Enter a bank name.'];
  else if (bankName.trim().length > 120) errors['bankName'] = ['Bank name must be 120 characters or fewer.'];
  if (!/^[A-Za-z0-9]{4}$/.test(accountNumberLastFour.trim())) {
    errors['lastFour'] = ['Enter exactly four letters or digits.'];
  }
  if (!Number.isSafeInteger(chartOfAccountId) || !chartOfAccountId || chartOfAccountId < 1) {
    errors['chartAccount'] = ['Select a ledger asset account.'];
  }
  return errors;
}

function validateAllocation(amount: number | undefined, maximum: number): string[] {
  if (typeof amount !== 'number' || !Number.isFinite(amount) || amount <= 0) {
    return ['Enter an allocation greater than zero.'];
  }
  if (amount > maximum) {
    return ['Allocation cannot exceed the remaining statement or ledger amount.'];
  }
  return [];
}

function validateAdjustment(
  offsetAccountId: number | null,
  entryDate: string,
  description: string,
): FormValidationErrors {
  const errors: FormValidationErrors = {};
  if (!Number.isSafeInteger(offsetAccountId) || !offsetAccountId || offsetAccountId < 1) {
    errors['offsetAccount'] = ['Select an offset account.'];
  }
  if (!isDateOnly(entryDate)) errors['adjustmentDate'] = ['Enter a valid posting date.'];
  if (description.length > 500) {
    errors['adjustmentDescription'] = ['Description must be 500 characters or fewer.'];
  }
  return errors;
}

function serverValidationErrors(error: unknown): FormValidationErrors {
  if (!(error instanceof HttpErrorResponse) ||
    (error.status !== 400 && error.status !== 422) ||
    !error.error || typeof error.error !== 'object' ||
    !('errors' in error.error) || !error.error.errors || typeof error.error.errors !== 'object') {
    return {};
  }

  return Object.fromEntries(
    Object.entries(error.error.errors).flatMap(([field, messages]) => {
      const fieldMessages = Array.isArray(messages)
        ? messages.filter((message): message is string => typeof message === 'string' && !!message.trim())
        : typeof messages === 'string' && messages.trim()
          ? [messages]
          : [];
      return fieldMessages.length ? [[field, fieldMessages]] : [];
    }),
  );
}

function isDateOnly(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const date = new Date(`${value}T00:00:00.000Z`);
  return !Number.isNaN(date.valueOf()) && date.toISOString().slice(0, 10) === value;
}
