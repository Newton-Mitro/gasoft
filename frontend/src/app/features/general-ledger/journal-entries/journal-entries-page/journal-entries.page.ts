import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { JournalEntryPostingService } from '../journal-entry-posting.service';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { PageChange, PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import {
  Choice,
  Field,
  JournalEntryLineForm,
  RecordFormMode,
} from '../../../../shared/models/shared.models';
import { RecordFormComponent } from '../../../../shared/components/record-form/record-form.component';
import { isBalancedJournalEntry } from '../journal-entry-lines';
import { OptionDataService } from '../../../../shared/data/option-data.service';
import {
  JournalEntry,
  JournalEntryPayload,
} from '../journal-entry.model';
import { JournalEntryService } from '../journal-entry.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { NotificationInboxService } from '../../../../layout/shell/notification-inbox.service';

const JOURNAL_ENTRY_OPTIONS = ['journals', 'periods', 'currencies', 'accounts'];

@Component({
  selector: 'app-journal-entries-page',
  imports: [RouterLink, RecordFormComponent, PaginationComponent],
  templateUrl: './journal-entries.page.html',
  styleUrl: '../../../../shared/styles/feature-page.css',
})
export class JournalEntriesPageComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  readonly notificationInbox = inject(NotificationInboxService);
  readonly pageKey = 'journal-entries';
  readonly title = 'Journal entries';
  readonly description = 'Create a balanced journal draft and post it to the general ledger.';
  readonly indexPath = '/general-ledger/journal-entries';
  readonly fields: Field[] = [
    { key: 'journalId', label: 'Journal', type: 'select', required: true, optionSource: 'journals' },
    { key: 'accountingPeriodId', label: 'Accounting period', type: 'select', required: true, optionSource: 'periods' },
    { key: 'entryDate', label: 'Entry date', type: 'date', required: true },
    { key: 'description', label: 'Entry description', required: true },
    { key: 'transactionCurrencyCode', label: 'Currency code', type: 'select', required: true, optionSource: 'currencies' },
    { key: 'exchangeRate', label: 'Exchange rate', type: 'number', required: true },
  ];
  rows: JournalEntry[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  form: Record<string, unknown> = {};
  options: Record<string, Choice[]> = {};
  optionsLoading: string[] = [];
  optionsFailed: string[] = [];
  lines: JournalEntryLineForm[] = [];
  loading = false;
  saving = false;
  error = '';
  view: 'index' | 'create' | 'edit' = 'index';
  formMode: RecordFormMode = 'create';
  formReady = false;
  private currentRecordId: number | null = null;
  private routeSubscription?: Subscription;
  private optionSubscriptions = new Subscription();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly entries: JournalEntryService,
    private readonly ledger: JournalEntryPostingService,
    private readonly optionData: OptionDataService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = combineLatest([this.route.data, this.route.paramMap]).subscribe(([data, params]) => {
      this.cancelOptionLoads();
      const view = data['view'];
      this.view = view === 'create' || view === 'edit' ? view : 'index';
      this.currentRecordId = toRecordId(params);
      this.error = '';
      if (this.view === 'create') {
        this.form = { exchangeRate: 1 };
        this.lines = [newJournalEntryLine(), newJournalEntryLine()];
        this.formReady = true;
        this.loading = false;
        this.loadOptions();
      } else if (this.view === 'edit') {
        this.formReady = false;
        this.loadOptions();
        this.loadRecord();
      } else {
        this.loadRows();
      }
    });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
    this.cancelOptionLoads();
  }

  get journalEntryBalanced(): boolean {
    return isBalancedJournalEntry(this.lines);
  }

  get formModeValue(): RecordFormMode {
    return this.view === 'edit' ? 'edit' : 'create';
  }

  retryOptions(): void {
    this.loadOptions();
  }

  setFormValue(key: string, value: unknown): void {
    this.form[key] = value;
  }

  setLines(lines: JournalEntryLineForm[]): void {
    this.lines = lines;
  }

  submit(): void {
    const journalId = Number(this.form['journalId']);
    const accountingPeriodId = Number(this.form['accountingPeriodId']);
    const exchangeRate = Number(this.form['exchangeRate']);
    const description = String(this.form['description'] ?? '').trim();
    const currencyCode = String(this.form['transactionCurrencyCode'] ?? '').trim();
    const entryDate = String(this.form['entryDate'] ?? '');
    if (!isPositiveId(journalId) || !isPositiveId(accountingPeriodId) ||
      !entryDate || !description || !currencyCode ||
      !Number.isFinite(exchangeRate) || exchangeRate <= 0 ||
      !this.lines.every(line => isPositiveId(Number(line.accountId))) ||
      !this.journalEntryBalanced) {
      this.error = 'Complete the journal details and add at least two valid, balanced entry lines.';
      return;
    }

    const payload: JournalEntryPayload = {
      journalId,
      accountingPeriodId,
      entryDate,
      description,
      lines: this.lines.map(line => ({
        accountId: Number(line.accountId),
        transactionCurrencyCode: currencyCode,
        debit: Number(line.debit ?? 0),
        credit: Number(line.credit ?? 0),
        exchangeRate,
        description,
      })),
    };
    if (this.view === 'edit' && this.currentRecordId === null) {
      this.error = 'A valid journal entry ID is required to update this record.';
      return;
    }
    const request = this.view === 'edit' && this.currentRecordId !== null
      ? this.entries.update(this.currentRecordId, payload)
      : this.entries.create(payload);
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
        this.notifications.error(errorMessage(error, 'Could not save this journal entry.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  postEntry(entry: JournalEntry): void {
    if (entry.status !== 'Draft') return;
    this.saving = true;
    this.error = '';
    this.ledger.postJournalEntry(entry.id).subscribe({
      next: posted => {
        this.rows = this.rows.map(row => row.id === posted.id
          ? { ...row, status: posted.status }
          : row);
        this.saving = false;
        this.notifications.success(`Journal entry ${posted.id} posted successfully.`);
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not post this journal entry.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  async reverseEntry(entry: JournalEntry): Promise<void> {
    if (entry.status !== 'Posted' || entry.reversedByEntryId || this.saving) return;
    const confirmed = await this.notifications.confirm({
      title: 'Reverse this posted journal entry?',
      text: 'A new journal entry will be posted today with the debit and credit sides swapped. The original entry will remain unchanged.',
      confirmButtonText: 'Reverse and post',
      confirmButtonVariant: 'danger',
    });
    if (!confirmed) return;

    this.saving = true;
    this.error = '';
    this.entries.reverse(entry.id, localDateOnly()).subscribe({
      next: reversal => {
        this.rows = this.rows.map(row => row.id === entry.id
          ? { ...row, reversedByEntryId: reversal.id }
          : row);
        this.saving = false;
        this.notifications.success(`Journal entry ${entry.id} was reversed by entry ${reversal.id}.`);
        this.notificationInbox.load(1);
        this.loadRows();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not reverse this journal entry.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  formatAmount(value: number | string): string {
    const amount = Number(value);
    if (!Number.isFinite(amount)) return '—';
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: 'USD',
      maximumFractionDigits: 2,
    }).format(amount);
  }

  onPageChange(change: PageChange): void {
    this.page = change.page;
    this.pageSize = change.pageSize;
    this.loadRows();
  }

  private loadRows(): void {
    this.loading = true;
    this.entries.getPage(this.page, this.pageSize).subscribe({
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
        this.notifications.error(errorMessage(error, 'Could not load journal entries.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.loading = false;
      this.error = 'A valid journal entry ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.entries.getById(this.currentRecordId).subscribe({
      next: entry => {
        if (entry.status !== 'Draft') {
          this.error = 'Only draft journal entries can be edited.';
          this.loading = false;
          this.changeDetector.markForCheck();
          return;
        }
        this.form = {
          journalId: entry.journalId,
          accountingPeriodId: entry.accountingPeriodId,
          entryDate: entry.entryDate,
          description: entry.description,
          transactionCurrencyCode: entry.lines[0]?.transactionCurrencyCode ?? '',
          exchangeRate: entry.lines[0]?.exchangeRate ?? 1,
        };
        this.lines = entry.lines.map(line => ({
          accountId: line.accountId,
          debit: Number(line.debit),
          credit: Number(line.credit),
        }));
        this.formReady = true;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load this journal entry.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadOptions(): void {
    this.cancelOptionLoads();
    this.error = '';
    this.options = {};
    this.optionsLoading = [...JOURNAL_ENTRY_OPTIONS];
    this.optionsFailed = [];
    for (const source of JOURNAL_ENTRY_OPTIONS) {
      const request = this.optionData.getOptions(source).subscribe({
        next: options => {
          this.options[source] = options;
          this.finishOptionLoad(source);
          this.changeDetector.markForCheck();
        },
        error: error => {
          this.finishOptionLoad(source);
          this.optionsFailed = [...this.optionsFailed, source];
          this.error = '';
          this.notifications.error(errorMessage(error, `Could not load ${source} options.`));
          this.changeDetector.markForCheck();
        },
      });
      this.optionSubscriptions.add(request);
    }
  }

  private finishOptionLoad(source: string): void {
    this.optionsLoading = this.optionsLoading.filter(option => option !== source);
  }

  private cancelOptionLoads(): void {
    this.optionSubscriptions.unsubscribe();
    this.optionSubscriptions = new Subscription();
  }
}

function newJournalEntryLine(): JournalEntryLineForm {
  return { accountId: null, debit: null, credit: null };
}

function toRecordId(params: ParamMap): number | null {
  const id = Number(params.get('recordId'));
  return isPositiveId(id) ? id : null;
}

function isPositiveId(value: number): boolean {
  return Number.isSafeInteger(value) && value > 0;
}

function localDateOnly(): string {
  const date = new Date();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}
