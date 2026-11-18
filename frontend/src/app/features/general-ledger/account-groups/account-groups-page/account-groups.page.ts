import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { RecordFormComponent } from '../../../../shared/components/record-form/record-form.component';
import { PaginationComponent, PageChange } from '../../../../shared/components/pagination/pagination.component';
import { Choice, Field, RecordFormMode } from '../../../../shared/models/shared.models';
import { AccountGroup, AccountGroupPayload, AccountType } from '../account-group.model';
import { AccountGroupService } from '../account-group.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { InputComponent } from '../../../../shared/components/input/input.component';

const ACCOUNT_TYPES: Choice[] = ['Asset', 'Liability', 'Equity', 'Revenue', 'Expense']
  .map(value => ({ label: value, value }));

@Component({
  selector: 'app-account-groups-page',
  imports: [FormsModule, RouterLink, RecordFormComponent, PaginationComponent, InputComponent],
  templateUrl: './account-groups.page.html',
  styleUrl: '../../../../shared/styles/feature-page.css',
})
export class AccountGroupsPageComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  readonly pageKey = 'account-groups';
  readonly title = 'Account groups';
  readonly description = 'Group accounts by their financial statement classification.';
  readonly fields: Field[] = [
    { key: 'code', label: 'Group code', required: true },
    { key: 'name', label: 'Group name', required: true },
    { key: 'accountType', label: 'Account type', type: 'select', required: true, options: ACCOUNT_TYPES },
  ];
  readonly indexPath = '/general-ledger/account-groups';
  rows: AccountGroup[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  form: Record<string, unknown> = {};
  loading = false;
  saving = false;
  error = '';
  search = '';
  view: 'index' | 'create' | 'edit' = 'index';
  private routeSubscription?: Subscription;
  private currentRecordId: number | null = null;
  private searchTimer?: ReturnType<typeof setTimeout>;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly groups: AccountGroupService,
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
    if (this.searchTimer) clearTimeout(this.searchTimer);
  }

  get formMode(): RecordFormMode {
    return this.view === 'edit' ? 'edit' : 'create';
  }

  onSearchChange(value: string): void {
    this.search = value;
    this.page = 1;
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.loadRows(), 300);
  }

  onPageChange(change: PageChange): void {
    this.page = change.page;
    this.pageSize = change.pageSize;
    this.loadRows();
  }

  setFormValue(key: string, value: unknown): void {
    this.form[key] = value;
  }

  submit(): void {
    const type = this.form['accountType'];
    if (!isAccountType(type)) {
      this.error = 'Choose a valid account type.';
      return;
    }
    const payload: AccountGroupPayload = {
      code: String(this.form['code'] ?? '').trim(),
      name: String(this.form['name'] ?? '').trim(),
      accountType: type,
    };

    let request;
    if (this.view === 'edit') {
      if (this.currentRecordId === null) {
        this.error = 'A valid account group ID is required to update this record.';
        return;
      }
      request = this.groups.update(this.currentRecordId, payload);
    } else {
      request = this.groups.create(payload);
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
        this.notifications.error(errorMessage(error, 'Could not save this account group.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  private loadRows(): void {
    this.loading = true;
    this.groups.getPage(this.search, this.page, this.pageSize).subscribe({
      next: page => {
        this.rows = page.items;
        this.page = page.page;
        this.pageSize = page.pageSize;
        this.totalCount = page.totalCount;
        this.totalPages = page.totalPages;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load account groups.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.error = 'A valid account group ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.groups.getById(this.currentRecordId).subscribe({
      next: group => {
        this.form = {
          code: group.code,
          name: group.name,
          accountType: group.accountType,
        };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load this account group.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}

function isAccountType(value: unknown): value is AccountType {
  return value === 'Asset' || value === 'Liability' || value === 'Equity' ||
    value === 'Revenue' || value === 'Expense';
}

function toRecordId(params: ParamMap): number | null {
  const id = Number(params.get('recordId'));
  return Number.isSafeInteger(id) && id > 0 ? id : null;
}
