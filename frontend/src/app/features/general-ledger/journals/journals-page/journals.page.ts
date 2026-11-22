import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { RecordFormComponent } from '../../../../shared/components/record-form/record-form.component';
import { PageChange, PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { Choice, Field, RecordFormMode } from '../../../../shared/models/shared.models';
import { Journal, JournalPayload, JournalType } from '../journal.model';
import { JournalService } from '../journal.service';
import { NotificationService } from '../../../../shared/services/notification.service';

const JOURNAL_TYPES: Choice[] = ['General', 'Sales', 'Purchase', 'Bank', 'Cash']
  .map(value => ({ label: value, value }));

@Component({
  selector: 'app-journals-page',
  imports: [RouterLink, RecordFormComponent, PaginationComponent],
  templateUrl: './journals.page.html',
  styleUrl: '../../../../shared/styles/feature-page.css',
})
export class JournalsPageComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  readonly pageKey = 'journals';
  readonly title = 'Journals';
  readonly description = 'Set up the journals used to record company activity.';
  readonly fields: Field[] = [
    { key: 'code', label: 'Journal code', required: true },
    { key: 'name', label: 'Journal name', required: true },
    { key: 'journalType', label: 'Journal type', type: 'select', required: true, options: JOURNAL_TYPES },
  ];
  readonly indexPath = '/general-ledger/journals';
  rows: Journal[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  form: Record<string, unknown> = {};
  loading = false;
  saving = false;
  error = '';
  view: 'index' | 'create' | 'edit' = 'index';
  private routeSubscription?: Subscription;
  private currentRecordId: number | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly journals: JournalService,
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
      } else if (this.view === 'edit') {
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

  get formFields(): Field[] {
    return this.view === 'edit'
      ? [...this.fields, { key: 'isActive', label: 'Active', type: 'checkbox' }]
      : this.fields;
  }

  setFormValue(key: string, value: unknown): void {
    this.form[key] = value;
  }

  submit(): void {
    const journalType = this.form['journalType'];
    if (!isJournalType(journalType)) {
      this.error = 'Choose a valid journal type.';
      return;
    }
    const payload: JournalPayload = {
      code: String(this.form['code'] ?? '').trim(),
      name: String(this.form['name'] ?? '').trim(),
      journalType,
      ...(this.view === 'edit' ? { isActive: Boolean(this.form['isActive']) } : {}),
    };

    let request;
    if (this.view === 'edit') {
      if (this.currentRecordId === null) {
        this.error = 'A valid journal ID is required to update this record.';
        return;
      }
      request = this.journals.update(this.currentRecordId, payload);
    } else {
      request = this.journals.create(payload);
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
        this.notifications.error(errorMessage(error, 'Could not save this journal.'));
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

  private loadRows(): void {
    this.loading = true;
    this.journals.getPage(this.page, this.pageSize).subscribe({
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
        this.notifications.error(errorMessage(error, 'Could not load journals.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.error = 'A valid journal ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.journals.getById(this.currentRecordId).subscribe({
      next: journal => {
        this.form = {
          code: journal.code,
          name: journal.name,
          journalType: journal.journalType,
          isActive: journal.isActive,
        };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load this journal.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}

function isJournalType(value: unknown): value is JournalType {
  return value === 'General' || value === 'Sales' || value === 'Purchase' || value === 'Bank' || value === 'Cash';
}

function toRecordId(params: ParamMap): number | null {
  const id = Number(params.get('recordId'));
  return Number.isSafeInteger(id) && id > 0 ? id : null;
}
