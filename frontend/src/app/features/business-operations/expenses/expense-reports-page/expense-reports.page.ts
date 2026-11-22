import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { combineLatest, firstValueFrom, forkJoin, Observable, of, Subscription } from 'rxjs';
import { ApiService } from '../../../../core/api.service';
import { AuthService } from '../../../../core/auth.service';
import { Choice } from '../../../../shared/models/shared.models';
import { OptionDataService } from '../../../../shared/data/option-data.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { FieldValidationDirective } from '../../../../shared/directives/field-validation.directive';
import { SearchableSelectDirective } from '../../../../shared/directives/searchable-select.directive';
import {
  FormValidationErrors,
  FormValidationService,
} from '../../../../shared/services/form-validation.service';
import {
  AccountingPeriodOption,
  ExpenseReport,
  ExpenseReportPayload,
} from '../expense-report.model';
import { ExpenseReportsService } from '../expense-reports.service';

interface ExpenseLineDraft {
  id: number | null;
  expenseDate: string;
  merchant: string;
  description: string;
  accountId: number | null;
  amount: number | null;
  receiptFile: File | null;
  receiptUrl: string | null;
  receiptObjectUrl: string | null;
  receiptPreviewUrl: SafeUrl | null;
}

interface JournalOption {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
}

type WorkflowMode = 'reject' | 'post' | 'reimburse';

@Component({
  selector: 'app-expense-reports-page',
  imports: [
    CurrencyPipe,
    FormsModule,
    FieldValidationDirective,
    SearchableSelectDirective,
    RouterLink,
  ],
  templateUrl: './expense-reports.page.html',
  styleUrls: ['../../../../shared/styles/feature-page.css', './expense-reports.page.css'],
})
export class ExpenseReportsPageComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly formValidation = inject(FormValidationService);
  reports: ExpenseReport[] = [];
  expenseAccounts: Choice[] = [];
  payableAccounts: Choice[] = [];
  cashAccounts: Choice[] = [];
  journals: Choice[] = [];
  periods: AccountingPeriodOption[] = [];
  loading = true;
  saving = false;
  busyReportId: number | null = null;
  error = '';

  editingReportId: number | null = null;
  editingTitle = '';
  expenseLines: ExpenseLineDraft[] = [];

  workflowReportId: number | null = null;
  workflowMode: WorkflowMode | null = null;
  workflowNote = '';
  workflowJournalId: number | null = null;
  workflowPeriodId: number | null = null;
  workflowAccountId: number | null = null;
  expandedReportId: number | null = null;
  view: 'index' | 'create' | 'edit' = 'index';

  private readonly auth = inject(AuthService);
  private routeSubscription?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly api: ApiService,
    private readonly optionData: OptionDataService,
    private readonly service: ExpenseReportsService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = combineLatest([this.route.data, this.route.paramMap]).subscribe(
      ([data, params]) => {
        this.clearReceiptPreviews();
        const view = data['view'];
        this.view = view === 'create' || view === 'edit' ? view : 'index';
        this.error = '';
        this.formValidation.clear();
        this.workflowReportId = null;
        this.workflowMode = null;

        if (!this.auth.companyId()) {
          this.loading = false;
          this.error = 'Choose a company before loading expense reports.';
          this.changeDetector.markForCheck();
          return;
        }

        if (this.view === 'index') {
          this.loadIndex();
        } else {
          this.loadEditor(this.view, params.get('reportId'));
        }
      },
    );
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
    this.clearReceiptPreviews();
  }

  get workflowReport(): ExpenseReport | undefined {
    return this.workflowReportId === null ? undefined : this.findReport(this.workflowReportId);
  }

  edit(report: ExpenseReport): void {
    if (!report.canEdit) return;
    void this.router.navigateByUrl(`/expenses/${report.id}/edit`);
  }

  addLine(): void {
    this.formValidation.clear();
    this.expenseLines = [...this.expenseLines, this.newLine(new Date().toISOString().slice(0, 10))];
  }

  removeLine(index: number): void {
    if (this.expenseLines.length <= 1) return;
    this.formValidation.clear();
    const removedLine = this.expenseLines[index];
    if (removedLine) this.clearReceiptPreview(removedLine);
    this.expenseLines = this.expenseLines.filter((_, lineIndex) => lineIndex !== index);
  }

  selectReceipt(index: number, event: Event): void {
    const input = event.target;
    if (!(input instanceof HTMLInputElement)) return;
    const line = this.expenseLines[index];
    if (!line) return;
    const file = input.files?.[0] ?? null;
    const field = `lines[${index}].receipt`;
    if (file && file.size > 5 * 1024 * 1024) {
      this.clearReceiptPreview(line);
      line.receiptFile = null;
      input.value = '';
      this.formValidation.setErrors({
        ...this.formValidation.errors(),
        [field]: ['Receipt files must be no larger than 5 MB.'],
      });
      return;
    }
    this.clearReceiptPreview(line);
    line.receiptFile = file;
    if (file && isPreviewableReceipt(file)) {
      const objectUrl = URL.createObjectURL(file);
      line.receiptObjectUrl = objectUrl;
      line.receiptPreviewUrl = this.sanitizer.bypassSecurityTrustUrl(objectUrl);
    }
    this.formValidation.clearField(field);
  }

  openReceiptPreview(objectUrl: string | null): void {
    if (!objectUrl) return;
    const opened = window.open(objectUrl, '_blank');
    if (!opened) {
      this.notifications.error('The PDF preview was blocked. Allow pop-ups to open it.');
      return;
    }
    opened.opener = null;
  }

  async saveReport(): Promise<void> {
    if (!this.validateDraft()) return;
    const reportId = this.editingReportId;
    const wasCreate = reportId === null;
    const payload: ExpenseReportPayload = {
      title: this.editingTitle.trim(),
      lines: this.expenseLines.map((line) => ({
        ...(line.id === null ? {} : { id: line.id }),
        expenseDate: line.expenseDate,
        merchant: line.merchant.trim(),
        description: line.description.trim(),
        accountId: Number(line.accountId),
        amount: Number(line.amount),
      })),
    };

    this.saving = true;
    this.error = '';
    let savedReport: ExpenseReport | null = null;
    try {
      let report =
        reportId === null
          ? await firstValueFrom(this.service.create(payload))
          : await firstValueFrom(this.service.update(reportId, payload));
      savedReport = report;
      this.editingReportId = report.id;
      this.expenseLines.forEach((line, index) => {
        line.id = report.lines[index]?.id ?? line.id;
        line.receiptUrl = report.lines[index]?.receiptUrl ?? line.receiptUrl;
      });
      for (let index = 0; index < this.expenseLines.length; index++) {
        const draft = this.expenseLines[index];
        const persistedLine = report.lines[index];
        if (!draft.receiptFile || !persistedLine) continue;
        report = await firstValueFrom(
          this.service.uploadReceipt(report.id, persistedLine.id, draft.receiptFile),
        );
        savedReport = report;
        const uploadedLine = report.lines.find((line) => line.id === persistedLine.id);
        this.clearReceiptPreview(draft);
        draft.receiptFile = null;
        draft.receiptUrl = uploadedLine?.receiptUrl ?? null;
      }

      this.replaceReport(report);
      this.editingReportId = null;
      this.clearReceiptPreviews();
      this.expenseLines = [];
      this.formValidation.clear();
      this.notifications.success(
        `Expense report ${wasCreate ? 'created' : 'updated'} successfully.`,
      );
      void this.router.navigateByUrl('/expenses');
    } catch (error) {
      if (savedReport) this.replaceReport(savedReport);
      const details = errorMessage(error, 'Could not save this expense report.');
      this.error = savedReport
        ? `The report draft was saved, but a receipt could not be uploaded. ${details}`
        : details;
      if (!savedReport) {
        const validationErrors = serverValidationErrors(error);
        if (Object.keys(validationErrors).length) {
          this.formValidation.setErrors(validationErrors);
          this.error = Object.keys(this.formValidation.unmatchedErrors()).length ? details : '';
        }
      }
      if (this.error) this.notifications.error(this.error);
      if (savedReport && wasCreate) {
        void this.router.navigateByUrl(`/expenses/${savedReport.id}/edit`);
      }
    } finally {
      this.saving = false;
      this.changeDetector.markForCheck();
    }
  }

  cancelEdit(): void {
    this.formValidation.clear();
    void this.router.navigateByUrl('/expenses');
  }

  cancelWorkflow(): void {
    this.formValidation.clear();
    this.workflowReportId = null;
    this.workflowMode = null;
  }

  submitReport(report: ExpenseReport): void {
    this.runAction(
      report,
      this.service.submit(report.id),
      'Expense report submitted for approval.',
    );
  }

  approve(report: ExpenseReport): void {
    this.runAction(report, this.service.approve(report.id, null), 'Expense report approved.');
  }

  beginWorkflow(report: ExpenseReport, mode: WorkflowMode): void {
    this.formValidation.clear();
    this.workflowReportId = report.id;
    this.workflowMode = mode;
    this.workflowNote = '';
    this.workflowJournalId = null;
    this.workflowPeriodId = this.periods[0]?.id ?? null;
    this.workflowAccountId = null;
    this.error = '';
  }

  completeWorkflow(): void {
    const reportId = this.workflowReportId;
    const mode = this.workflowMode;
    if (reportId === null || mode === null) return;

    this.formValidation.clear();
    if (mode === 'reject') {
      const note = this.workflowNote.trim();
      if (!note) {
        this.formValidation.setErrors({
          workflowNote: ['Enter a reason for rejecting this expense report.'],
        });
        return;
      }
      if (note.length > 1000) {
        this.formValidation.setErrors({
          workflowNote: ['Reason must be 1,000 characters or fewer.'],
        });
        return;
      }
      const report = this.findReport(reportId);
      if (report)
        this.runAction(report, this.service.reject(reportId, note), 'Expense report rejected.');
      return;
    }

    const validationErrors: FormValidationErrors = {};
    if (
      !isPositiveId(this.workflowJournalId) ||
      !this.journals.some((journal) => Number(journal.value) === this.workflowJournalId)
    ) {
      validationErrors['workflowJournalId'] = ['Select a journal.'];
    }
    if (!isPositiveId(this.workflowPeriodId)) {
      validationErrors['workflowPeriodId'] = ['Select an open accounting period.'];
    } else if (!this.periods.some((period) => period.id === this.workflowPeriodId)) {
      validationErrors['workflowPeriodId'] = ['Select an open accounting period covering today.'];
    }
    const accounts = mode === 'post' ? this.payableAccounts : this.cashAccounts;
    if (
      !isPositiveId(this.workflowAccountId) ||
      !accounts.some((account) => Number(account.value) === this.workflowAccountId)
    ) {
      validationErrors['workflowAccountId'] = [
        mode === 'post' ? 'Select an employee payable account.' : 'Select a cash or bank account.',
      ];
    }
    this.formValidation.setErrors(validationErrors);
    if (Object.keys(validationErrors).length) {
      return;
    }
    if (
      !isPositiveId(this.workflowJournalId) ||
      !isPositiveId(this.workflowPeriodId) ||
      !isPositiveId(this.workflowAccountId)
    ) {
      return;
    }
    const report = this.findReport(reportId);
    if (!report) return;
    const request =
      mode === 'post'
        ? this.service.post(
            reportId,
            this.workflowJournalId,
            this.workflowPeriodId,
            this.workflowAccountId,
          )
        : this.service.reimburse(
            reportId,
            this.workflowJournalId,
            this.workflowPeriodId,
            this.workflowAccountId,
          );
    this.runAction(
      report,
      request,
      mode === 'post'
        ? 'Expense report posted to the ledger.'
        : 'Expense report reimbursement posted.',
    );
  }

  async openReceipt(receiptUrl: string | null): Promise<void> {
    if (!receiptUrl) return;
    try {
      const receipt = await firstValueFrom(this.service.getReceipt(receiptUrl));
      const url = URL.createObjectURL(receipt);
      const opened = window.open(url, '_blank');
      if (opened) opened.opener = null;
      else {
        const link = document.createElement('a');
        link.href = url;
        link.download = 'expense-receipt';
        link.click();
      }
      window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch (error) {
      this.notifications.error(errorMessage(error, 'Could not open the receipt.'));
    }
  }

  private runAction(
    report: ExpenseReport,
    request: Observable<ExpenseReport>,
    successMessage: string,
  ): void {
    this.busyReportId = report.id;
    this.error = '';
    request.subscribe({
      next: (updated) => {
        this.replaceReport(updated);
        this.workflowReportId = null;
        this.workflowMode = null;
        this.notifications.success(successMessage);
        this.busyReportId = null;
        this.changeDetector.markForCheck();
      },
      error: (error) => {
        const details = errorMessage(error, 'Could not update this expense report.');
        const validationErrors = serverValidationErrors(error);
        if (Object.keys(validationErrors).length) {
          this.formValidation.setErrors(validationErrors);
          this.error = Object.keys(this.formValidation.unmatchedErrors()).length ? details : '';
        } else {
          this.error = details;
        }
        if (this.error) this.notifications.error(this.error);
        this.busyReportId = null;
        this.changeDetector.markForCheck();
      },
    });
  }

  private validateDraft(): boolean {
    this.formValidation.clear();
    const errors: FormValidationErrors = {};
    if (!this.editingTitle.trim()) errors['reportTitle'] = ['Enter a report title.'];
    else if (this.editingTitle.trim().length > 200) {
      errors['reportTitle'] = ['Report title must be 200 characters or fewer.'];
    }
    if (!this.expenseLines.length) errors['lines'] = ['Add at least one expense.'];

    this.expenseLines.forEach((line, index) => {
      const field = (name: string) => `lines[${index}].${name}`;
      if (!isDateOnly(line.expenseDate)) {
        errors[field('expenseDate')] = ['Enter a valid expense date.'];
      }
      if (!line.merchant.trim()) {
        errors[field('merchant')] = ['Enter a merchant.'];
      } else if (line.merchant.trim().length > 160) {
        errors[field('merchant')] = ['Merchant must be 160 characters or fewer.'];
      }
      if (!line.description.trim()) {
        errors[field('description')] = ['Enter an expense description.'];
      } else if (line.description.trim().length > 500) {
        errors[field('description')] = ['Description must be 500 characters or fewer.'];
      }
      if (!isPositiveId(line.accountId)) {
        errors[field('accountId')] = ['Select an expense account.'];
      }
      if (line.amount === null || !Number.isFinite(Number(line.amount)) || line.amount <= 0) {
        errors[field('amount')] = ['Amount must be greater than zero.'];
      }
      if (line.receiptFile && line.receiptFile.size > 5 * 1024 * 1024) {
        errors[field('receipt')] = ['Receipt files must be no larger than 5 MB.'];
      }
    });

    this.formValidation.setErrors(errors);
    return Object.keys(errors).length === 0;
  }

  fieldMessages(field: string): string[] {
    return this.formValidation.messagesFor(field);
  }

  private replaceReport(report: ExpenseReport): void {
    const index = this.reports.findIndex((item) => item.id === report.id);
    this.reports =
      index < 0
        ? [report, ...this.reports]
        : this.reports.map((item) => (item.id === report.id ? report : item));
  }

  private findReport(id: number): ExpenseReport | undefined {
    return this.reports.find((report) => report.id === id);
  }

  private newLine(expenseDate: string): ExpenseLineDraft {
    return {
      id: null,
      expenseDate,
      merchant: '',
      description: '',
      accountId: null,
      amount: null,
      receiptFile: null,
      receiptUrl: null,
      receiptObjectUrl: null,
      receiptPreviewUrl: null,
    };
  }

  private clearReceiptPreview(line: ExpenseLineDraft): void {
    if (line.receiptObjectUrl) URL.revokeObjectURL(line.receiptObjectUrl);
    line.receiptObjectUrl = null;
    line.receiptPreviewUrl = null;
  }

  private clearReceiptPreviews(): void {
    this.expenseLines.forEach((line) => this.clearReceiptPreview(line));
  }

  private loadIndex(): void {
    this.loading = true;
    forkJoin({
      reports: this.service.getAll(),
      payableAccounts: this.optionData.getAccountOptionsByType('Liability'),
      cashAccounts: this.optionData.getAccountOptionsByType('Asset'),
      journals: this.api.get<JournalOption[]>('journals'),
      periods: this.api.get<AccountingPeriodOption[]>('accounting-periods'),
    }).subscribe({
      next: (result) => {
        this.reports = result.reports;
        this.payableAccounts = result.payableAccounts;
        this.cashAccounts = result.cashAccounts;
        this.journals = result.journals
          .filter((journal) => journal.isActive)
          .map((journal) => ({ label: `${journal.code} · ${journal.name}`, value: journal.id }));
        const today = new Date().toISOString().slice(0, 10);
        this.periods = result.periods.filter(
          (period) =>
            period.status === 'Open' && period.startsOn <= today && period.endsOn >= today,
        );
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: (error) => {
        this.error = errorMessage(error, 'Could not load expense reports and accounting setup.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadEditor(view: 'create' | 'edit', reportId: string | null): void {
    this.loading = true;
    this.editingReportId = null;
    this.editingTitle = '';
    this.expenseLines = [];
    const reportIdValue = reportId === null ? null : Number(reportId);
    if (view === 'edit' && (!Number.isSafeInteger(reportIdValue) || Number(reportIdValue) <= 0)) {
      this.error = 'A valid expense report ID is required to edit this report.';
      this.loading = false;
      this.changeDetector.markForCheck();
      return;
    }

    forkJoin({
      expenseAccounts: this.optionData.getAccountOptionsByType('Expense'),
      reports: view === 'edit' ? this.service.getAll() : of([] as ExpenseReport[]),
    }).subscribe({
      next: (result) => {
        this.expenseAccounts = result.expenseAccounts;
        if (view === 'create') {
          const today = new Date().toISOString().slice(0, 10);
          this.editingTitle = `Expense report - ${today}`;
          this.expenseLines = [this.newLine(today)];
        } else {
          const report = result.reports.find((item) => item.id === reportIdValue);
          if (!report || !report.canEdit) {
            this.error = report
              ? 'This expense report cannot be edited.'
              : 'Expense report not found.';
            this.loading = false;
            this.changeDetector.markForCheck();
            return;
          }
          this.editingReportId = report.id;
          this.editingTitle = report.title;
          this.expenseLines = report.lines.map((line) => ({
            id: line.id,
            expenseDate: line.expenseDate,
            merchant: line.merchant,
            description: line.description,
            accountId: line.accountId,
            amount: line.amount,
            receiptFile: null,
            receiptUrl: line.receiptUrl,
            receiptObjectUrl: null,
            receiptPreviewUrl: null,
          }));
        }
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: (error) => {
        this.error = errorMessage(error, 'Could not load the expense report form.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}

function isPositiveId(value: number | null): value is number {
  return value !== null && Number.isSafeInteger(value) && value > 0;
}

function isDateOnly(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const date = new Date(`${value}T00:00:00.000Z`);
  return !Number.isNaN(date.valueOf()) && date.toISOString().slice(0, 10) === value;
}

function isPreviewableReceipt(file: File): boolean {
  return ['image/jpeg', 'image/png', 'image/webp', 'application/pdf'].includes(file.type);
}

function serverValidationErrors(error: unknown): FormValidationErrors {
  if (
    !(error instanceof HttpErrorResponse) ||
    (error.status !== 400 && error.status !== 422) ||
    !error.error ||
    typeof error.error !== 'object' ||
    !('errors' in error.error) ||
    !error.error.errors ||
    typeof error.error.errors !== 'object'
  ) {
    return {};
  }

  return Object.fromEntries(
    Object.entries(error.error.errors).flatMap(([field, messages]) => {
      const fieldMessages = Array.isArray(messages)
        ? messages.filter(
            (message): message is string => typeof message === 'string' && !!message.trim(),
          )
        : typeof messages === 'string' && messages.trim()
          ? [messages]
          : [];
      return fieldMessages.length ? [[field, fieldMessages]] : [];
    }),
  );
}
