import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { RecordFormComponent } from '../../../../shared/components/record-form/record-form.component';
import { PageChange, PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { Choice, Field, RecordFormMode } from '../../../../shared/models/shared.models';
import { OptionDataService } from '../../../../shared/data/option-data.service';
import {
  AccountingPeriod,
  AccountingPeriodCloseChecklist,
  AccountingPeriodPayload,
  AccountingPeriodStatusChange,
} from '../period.model';
import { AccountingPeriodService } from '../period.service';
import { NotificationService } from '../../../../shared/services/notification.service';

@Component({
  selector: 'app-periods-page',
  imports: [RouterLink, RecordFormComponent, PaginationComponent],
  templateUrl: './periods.page.html',
  styleUrl: '../../../../shared/styles/feature-page.css',
})
export class PeriodsPageComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  readonly pageKey = 'periods';
  readonly title = 'Accounting periods';
  readonly description = 'Manage the posting windows within each fiscal year.';
  readonly fields: Field[] = [
    { key: 'fiscalYearId', label: 'Fiscal year', type: 'select', required: true, optionSource: 'years' },
    { key: 'startsOn', label: 'Starts on', type: 'date', required: true },
    { key: 'endsOn', label: 'Ends on', type: 'date', required: true },
  ];
  readonly indexPath = '/general-ledger/periods';
  rows: AccountingPeriod[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  form: Record<string, unknown> = {};
  options: Record<string, Choice[]> = {};
  loading = false;
  saving = false;
  busyPeriodId: number | null = null;
  historyPeriodId: number | null = null;
  statusHistory: AccountingPeriodStatusChange[] = [];
  closeChecklist: AccountingPeriodCloseChecklist | null = null;
  closeChecklistPeriodId: number | null = null;
  error = '';
  view: 'index' | 'create' | 'edit' = 'index';
  private routeSubscription?: Subscription;
  private currentRecordId: number | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly periods: AccountingPeriodService,
    private readonly optionData: OptionDataService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = combineLatest([this.route.data, this.route.paramMap]).subscribe(([data, params]) => {
      const view = data['view'];
      this.view = view === 'create' || view === 'edit' ? view : 'index';
      this.currentRecordId = toRecordId(params);
      this.error = '';
      if (this.view === 'create') {
        this.form = {};
        this.loadYearOptions();
      } else if (this.view === 'edit') {
        this.loadYearOptions();
        this.loadRecord();
      } else {
        this.loadRows();
      }
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
    const payload: AccountingPeriodPayload = {
      fiscalYearId: Number(this.form['fiscalYearId']),
      startsOn: String(this.form['startsOn'] ?? ''),
      endsOn: String(this.form['endsOn'] ?? ''),
    };
    let request;
    if (this.view === 'edit') {
      if (this.currentRecordId === null) {
        this.error = 'A valid accounting period ID is required to update this record.';
        return;
      }
      request = this.periods.update(this.currentRecordId, payload);
    } else {
      request = this.periods.create(payload);
    }

    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.saving = false;
        this.notifications.success(`${this.title} ${this.view === 'edit' ? 'updated' : 'created'} successfully.`);
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not save this accounting period.'));
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

  async changeStatus(period: AccountingPeriod, action: 'close' | 'reopen'): Promise<void> {
    if (action === 'close') {
      this.loadCloseChecklist(period);
      return;
    }

    const confirmed = await this.notifications.confirm({
      title: 'Reopen this accounting period?',
      text: 'Posting will be allowed again.',
      confirmButtonText: 'Reopen period',
    });
    if (!confirmed) return;
    const reason = await this.notifications.prompt({
      title: 'Reason for reopening',
      text: `Enter a reason to ${action} this period.`,
      requiredMessage: 'A reason is required to change an accounting period status.',
    });
    if (reason === null) return;
    this.submitStatusChange(period, action, reason);
  }

  private loadCloseChecklist(period: AccountingPeriod): void {
    this.error = '';
    this.busyPeriodId = period.id;
    this.closeChecklistPeriodId = period.id;
    this.closeChecklist = null;
    this.periods.getCloseChecklist(period.id).subscribe({
      next: async checklist => {
        this.closeChecklist = checklist;
        this.busyPeriodId = null;
        if (!checklist.canClose) {
          this.error = this.closeChecklistMessage(checklist);
          this.changeDetector.markForCheck();
          return;
        }
        const confirmed = await this.notifications.confirm({
          title: 'Close this accounting period?',
          text: 'The period close checklist is clear.',
          confirmButtonText: 'Close period',
        });
        if (!confirmed) {
          this.changeDetector.markForCheck();
          return;
        }
        const reason = await this.notifications.prompt({
          title: 'Reason for closing',
          text: 'Enter a reason to close this accounting period.',
          requiredMessage: 'A reason is required to close an accounting period.',
        });
        if (reason === null) {
          this.changeDetector.markForCheck();
          return;
        }
        this.submitStatusChange(period, 'close', reason);
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load the period close checklist.'));
        this.busyPeriodId = null;
        this.changeDetector.markForCheck();
      },
    });
  }

  private submitStatusChange(period: AccountingPeriod, action: 'close' | 'reopen', reason: string): void {
    this.error = '';
    this.busyPeriodId = period.id;
    const request = action === 'close'
      ? this.periods.close(period.id, reason)
      : this.periods.reopen(period.id, reason);
    request.subscribe({
      next: () => {
        this.busyPeriodId = null;
        this.notifications.success(`Accounting period ${action === 'close' ? 'closed' : 'reopened'} successfully.`);
        this.historyPeriodId = null;
        this.statusHistory = [];
        this.closeChecklistPeriodId = null;
        this.closeChecklist = null;
        this.loadRows();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, `Could not ${action} this accounting period.`));
        this.busyPeriodId = null;
        this.changeDetector.markForCheck();
      },
    });
  }

  private closeChecklistMessage(checklist: AccountingPeriodCloseChecklist): string {
    const blockers = [];
    if (checklist.draftJournalEntryCount > 0)
      blockers.push(`${checklist.draftJournalEntryCount} draft journal entries`);
    if (checklist.unreconciledBankTransactionCount > 0)
      blockers.push(`${checklist.unreconciledBankTransactionCount} unreconciled bank statement transactions`);
    return `Resolve the period close checklist before closing: ${blockers.join(' and ')}.`;
  }

  toggleStatusHistory(periodId: number): void {
    if (this.historyPeriodId === periodId) {
      this.historyPeriodId = null;
      this.statusHistory = [];
      return;
    }
    this.historyPeriodId = periodId;
    this.statusHistory = [];
    this.periods.getStatusHistory(periodId).subscribe({
      next: history => {
        this.statusHistory = history;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load accounting period status history.'));
        this.changeDetector.markForCheck();
      },
    });
  }

  formatStatusChangeDate(value: string): string {
    return new Intl.DateTimeFormat(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }

  private loadYearOptions(): void {
    this.optionData.getOptions('years').subscribe({
      next: options => {
        this.options = { ...this.options, years: options };
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load fiscal year options.'));
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRows(): void {
    this.loading = true;
    this.periods.getPage(this.page, this.pageSize).subscribe({
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
        this.notifications.error(errorMessage(error, 'Could not load accounting periods.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.error = 'A valid accounting period ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.periods.getById(this.currentRecordId).subscribe({
      next: period => {
        this.form = {
          fiscalYearId: period.fiscalYearId,
          startsOn: period.startsOn,
          endsOn: period.endsOn,
        };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load this accounting period.'));
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
