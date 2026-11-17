import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { PageChange, PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { Choice, Field, RecordFormMode } from '../../../shared/models/shared.models';
import { OptionDataService } from '../../../shared/data/option-data.service';
import {
  FiscalYear,
  FiscalYearCloseChecklist,
  FiscalYearPayload,
  FiscalYearStatusChange,
} from './fiscal-year.model';
import { FiscalYearService } from './fiscal-year.service';
import { NotificationService } from '../../../shared/services/notification.service';

@Component({
  selector: 'app-fiscal-years-page',
  imports: [FormsModule, RouterLink, RecordFormComponent, PaginationComponent],
  templateUrl: './fiscal-years.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class FiscalYearsPageComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  readonly pageKey = 'fiscal-years';
  readonly title = 'Fiscal years';
  readonly description = 'Review the financial year calendar used by your company.';
  readonly fields: Field[] = [
    { key: 'startsOn', label: 'Starts on', type: 'date', required: true },
    { key: 'endsOn', label: 'Ends on', type: 'date', required: true },
  ];
  readonly indexPath = '/general-ledger/fiscal-years';
  rows: FiscalYear[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  form: Record<string, unknown> = {};
  loading = false;
  saving = false;
  error = '';
  success = '';
  busyYearId: number | null = null;
  checklistYearId: number | null = null;
  closeChecklist: FiscalYearCloseChecklist | null = null;
  historyYearId: number | null = null;
  statusHistory: FiscalYearStatusChange[] = [];
  equityAccountOptions: Choice[] = [];
  retainedEarningsAccountId: number | null = null;
  view: 'index' | 'create' | 'edit' = 'index';
  private routeSubscription?: Subscription;
  private currentRecordId: number | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly fiscalYears: FiscalYearService,
    private readonly optionData: OptionDataService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = combineLatest([this.route.data, this.route.paramMap]).subscribe(([data, params]) => {
      const view = data['view'];
      this.view = view === 'create' || view === 'edit' ? view : 'index';
      this.currentRecordId = toRecordId(params);
      this.error = '';
      this.success = '';
      if (this.view === 'create') {
        this.form = {};
        return;
      }
      if (this.view === 'edit') {
        this.loadRecord();
        return;
      }
      this.loadEquityAccountOptions();
      this.loadRows();
    });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
  }

  get formMode(): RecordFormMode {
    return this.view === 'edit' ? 'edit' : 'create';
  }

  setFormValue(key: string, value: unknown): void {
    this.form[key] = value;
  }

  submit(): void {
    const payload: FiscalYearPayload = {
      startsOn: String(this.form['startsOn'] ?? ''),
      endsOn: String(this.form['endsOn'] ?? ''),
    };
    this.saving = true;
    this.error = '';
    let request;
    if (this.view === 'edit') {
      if (this.currentRecordId === null) {
        this.error = 'A valid fiscal year ID is required to update this record.';
        this.saving = false;
        return;
      }
      request = this.fiscalYears.update(this.currentRecordId, payload);
    } else {
      request = this.fiscalYears.create(payload);
    }
    request.subscribe({
      next: () => {
        this.saving = false;
        this.notifications.success(`${this.title} ${this.view === 'edit' ? 'updated' : 'created'} successfully.`);
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not save this fiscal year.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  onPageChange(change: PageChange): void {
    this.page = change.page;
    this.pageSize = change.pageSize;
    this.loadRows();
  }

  async changeStatus(year: FiscalYear, action: 'close' | 'reopen'): Promise<void> {
    if (action === 'close') {
      this.loadCloseChecklist(year);
      return;
    }
    const confirmed = await this.notifications.confirm({
      title: 'Reopen this fiscal year?',
      text: 'Its accounting periods will remain closed.',
      confirmButtonText: 'Reopen fiscal year',
    });
    if (!confirmed) return;
    const reason = await this.notifications.prompt({
      title: 'Reason for reopening',
      text: 'Enter a reason to reopen this fiscal year.',
      requiredMessage: 'A reason is required to change a fiscal-year status.',
    });
    if (reason === null) return;
    this.submitStatusChange(year, action, reason);
  }

  toggleStatusHistory(fiscalYearId: number): void {
    if (this.historyYearId === fiscalYearId) {
      this.historyYearId = null;
      this.statusHistory = [];
      return;
    }
    this.historyYearId = fiscalYearId;
    this.statusHistory = [];
    this.fiscalYears.getStatusHistory(fiscalYearId).subscribe({
      next: history => {
        this.statusHistory = history;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load fiscal-year status history.'));
        this.changeDetector.markForCheck();
      },
    });
  }

  formatStatusChangeDate(value: string): string {
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
  }

  formatClosingAmount(value: number): string {
    return new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
  }

  private loadCloseChecklist(year: FiscalYear): void {
    this.error = '';
    this.busyYearId = year.id;
    this.checklistYearId = year.id;
    this.closeChecklist = null;
    this.fiscalYears.getCloseChecklist(year.id).subscribe({
      next: async checklist => {
        this.closeChecklist = checklist;
        this.busyYearId = null;
        if (!checklist.canClose) {
          this.error = this.closeChecklistMessage(checklist);
          this.changeDetector.markForCheck();
          return;
        }
        const confirmed = await this.notifications.confirm({
          title: 'Close this fiscal year?',
          text: 'All accounting periods are closed and there are no draft journals.',
          confirmButtonText: 'Close fiscal year',
        });
        if (!confirmed) {
          this.changeDetector.markForCheck();
          return;
        }
        const reason = await this.notifications.prompt({
          title: 'Reason for closing',
          text: 'Enter a reason to close this fiscal year.',
          requiredMessage: 'A reason is required to close a fiscal year.',
        });
        if (reason === null) {
          this.changeDetector.markForCheck();
          return;
        }
        this.submitStatusChange(year, 'close', reason);
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load the fiscal-year close checklist.'));
        this.busyYearId = null;
        this.changeDetector.markForCheck();
      },
    });
  }

  async prepareClosingJournal(year: FiscalYear): Promise<void> {
    if (this.retainedEarningsAccountId === null) {
      this.error = 'Choose an active Equity account for retained earnings.';
      return;
    }
    const confirmed = await this.notifications.confirm({
      title: 'Prepare year-end closing journal?',
      text: 'This creates a draft dated on the fiscal year-end. All periods must already be closed. Review and post the draft before closing the fiscal year.',
      confirmButtonText: 'Prepare journal',
      icon: 'question',
    });
    if (!confirmed) return;
    this.error = '';
    this.busyYearId = year.id;
    this.fiscalYears.prepareClosingJournal(year.id, this.retainedEarningsAccountId).subscribe({
      next: entry => {
        this.busyYearId = null;
        this.notifications.success(entry.status === 'Posted'
          ? `Year-end closing journal ${entry.id} is already posted.`
          : `Draft year-end closing journal ${entry.id} created. Review and post it before closing the fiscal year.`);
        this.success = '';
        this.loadCloseChecklist(year);
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not prepare the year-end closing journal.'));
        this.busyYearId = null;
        this.changeDetector.markForCheck();
      },
    });
  }

  async prepareOpeningBalances(year: FiscalYear): Promise<void> {
    const confirmed = await this.notifications.confirm({
      title: 'Prepare opening balances?',
      text: 'This creates a draft journal using the prior year’s posted balance-sheet balances. Review and post the draft before use.',
      confirmButtonText: 'Prepare opening balances',
      icon: 'question',
    });
    if (!confirmed) return;
    this.error = '';
    this.busyYearId = year.id;
    this.fiscalYears.prepareOpeningBalances(year.id).subscribe({
      next: entry => {
        this.busyYearId = null;
        this.notifications.success(`Draft opening-balance journal ${entry.id} created for ${year.startsOn}. Review and post it before using the new year.`);
        this.success = '';
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not prepare the opening balances.'));
        this.busyYearId = null;
        this.changeDetector.markForCheck();
      },
    });
  }

  async postClosingJournal(year: FiscalYear, journalEntryId: number): Promise<void> {
    const confirmed = await this.notifications.confirm({
      title: 'Post the year-end closing journal?',
      text: 'This will zero the income and expense accounts for the fiscal year and transfer net income or loss to retained earnings.',
      confirmButtonText: 'Post closing journal',
    });
    if (!confirmed) return;
    this.error = '';
    this.busyYearId = year.id;
    this.fiscalYears.postClosingJournal(journalEntryId).subscribe({
      next: () => {
        this.busyYearId = null;
        this.notifications.success(`Year-end closing journal ${journalEntryId} posted.`);
        this.success = '';
        this.loadCloseChecklist(year);
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not post the year-end closing journal.'));
        this.busyYearId = null;
        this.changeDetector.markForCheck();
      },
    });
  }

  private submitStatusChange(year: FiscalYear, action: 'close' | 'reopen', reason: string): void {
    this.error = '';
    this.busyYearId = year.id;
    const request = action === 'close'
      ? this.fiscalYears.close(year.id, reason)
      : this.fiscalYears.reopen(year.id, reason);
    request.subscribe({
      next: () => {
        this.busyYearId = null;
        this.notifications.success(`Fiscal year ${action === 'close' ? 'closed' : 'reopened'} successfully.`);
        this.checklistYearId = null;
        this.closeChecklist = null;
        this.historyYearId = null;
        this.statusHistory = [];
        this.loadRows();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, `Could not ${action} this fiscal year.`));
        this.busyYearId = null;
        this.changeDetector.markForCheck();
      },
    });
  }

  private closeChecklistMessage(checklist: FiscalYearCloseChecklist): string {
    const blockers = [];
    if (checklist.accountingPeriodCount === 0) blockers.push('the year has no accounting periods');
    if (checklist.openAccountingPeriodCount > 0)
      blockers.push(`${checklist.openAccountingPeriodCount} open accounting periods`);
    if (checklist.draftJournalEntryCount > 0)
      blockers.push(`${checklist.draftJournalEntryCount} draft journal entries`);
    if (checklist.closingJournalRequired && checklist.closingJournalStatus !== 'Posted')
      blockers.push(checklist.closingJournalStatus === 'Draft'
        ? 'the year-end closing journal must be posted'
        : 'a year-end closing journal must be prepared and posted');
    return `Resolve the fiscal-year close checklist before closing: ${blockers.join(' and ')}.`;
  }

  private loadEquityAccountOptions(): void {
    this.optionData.getEquityAccountOptions().subscribe({
      next: options => {
        this.equityAccountOptions = options;
        const preferred = options.find(option => option.label.startsWith('3100 ·'))?.value;
        const selected = preferred ?? options[0]?.value;
        this.retainedEarningsAccountId =
          typeof selected === 'number' && Number.isSafeInteger(selected) ? selected : null;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load Equity accounts for year-end closing.'));
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRows(): void {
    this.loading = true;
    this.fiscalYears.getPage(this.page, this.pageSize).subscribe({
      next: result => {
        this.rows = result.items;
        this.page = result.page;
        this.pageSize = result.pageSize;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load fiscal years.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    const id = this.currentRecordId;
    if (id === null) {
      this.error = 'A valid fiscal year ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.fiscalYears.getById(id).subscribe({
      next: year => {
        this.form = { startsOn: year.startsOn, endsOn: year.endsOn };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load this fiscal year.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}

function toRecordId(params: ParamMap): number | null {
  const id = Number(params.get('recordId'));
  return Number.isSafeInteger(id) && id > 0 ? id : null;
}
