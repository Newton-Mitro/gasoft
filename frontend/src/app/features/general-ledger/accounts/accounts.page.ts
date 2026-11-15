import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { Choice, Field, RecordFormMode } from '../../../shared/models/shared.models';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { Account, AccountPayload } from './account.model';
import { AccountService } from './account.service';

@Component({
  selector: 'app-accounts-page',
  imports: [FormsModule, RouterLink, RecordFormComponent],
  templateUrl: './accounts.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class AccountsPageComponent implements OnInit, OnDestroy {
  readonly pageKey = 'accounts';
  readonly title = 'Chart of accounts';
  readonly description = 'Organize your ledger accounts and control where transactions can be posted.';
  readonly fields: Field[] = [
    { key: 'accountGroupId', label: 'Account group', type: 'select', required: true, optionSource: 'groups' },
    { key: 'code', label: 'Account code', required: true },
    { key: 'name', label: 'Account name', required: true },
    { key: 'parentId', label: 'Parent account ID', type: 'number' },
    { key: 'allowPosting', label: 'Allow postings', type: 'checkbox' },
  ];
  readonly indexPath = '/general-ledger/accounts';
  rows: Account[] = [];
  totalCount = 0;
  form: Record<string, unknown> = {};
  options: Record<string, Choice[]> = {};
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
    private readonly accounts: AccountService,
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
        this.form = { allowPosting: false };
        this.loadGroupOptions();
      } else if (this.view === 'edit') {
        this.loadGroupOptions();
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

  get formFields(): Field[] {
    return this.view === 'edit'
      ? [...this.fields, { key: 'isActive', label: 'Active', type: 'checkbox' }]
      : this.fields;
  }

  onSearchChange(value: string): void {
    this.search = value;
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.loadRows(), 300);
  }

  setFormValue(key: string, value: unknown): void {
    this.form[key] = value;
  }

  submit(): void {
    const accountGroupId = Number(this.form['accountGroupId']);
    const parentValue = this.form['parentId'];
    const payload: AccountPayload = {
      accountGroupId,
      code: String(this.form['code'] ?? '').trim(),
      name: String(this.form['name'] ?? '').trim(),
      parentId: parentValue === '' || parentValue === null || parentValue === undefined
        ? null
        : Number(parentValue),
      allowPosting: Boolean(this.form['allowPosting']),
      ...(this.view === 'edit' ? { isActive: Boolean(this.form['isActive']) } : {}),
    };
    if (!Number.isSafeInteger(payload.accountGroupId) || payload.accountGroupId < 1) {
      this.error = 'Choose a valid account group.';
      return;
    }
    if (payload.parentId !== null && (!Number.isSafeInteger(payload.parentId) || payload.parentId < 1)) {
      this.error = 'Enter a valid parent account ID or leave it blank.';
      return;
    }

    let request;
    if (this.view === 'edit') {
      if (this.currentRecordId === null) {
        this.error = 'A valid account ID is required to update this record.';
        return;
      }
      request = this.accounts.update(this.currentRecordId, payload);
    } else {
      request = this.accounts.create(payload);
    }

    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save this account.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  private loadGroupOptions(): void {
    this.optionData.getOptions('groups').subscribe({
      next: options => {
        this.options = { ...this.options, groups: options };
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load account group options.');
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRows(): void {
    this.loading = true;
    this.accounts.getPage(this.search).subscribe({
      next: page => {
        this.rows = page.items;
        this.totalCount = page.totalCount;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load accounts.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.error = 'A valid account ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.accounts.getById(this.currentRecordId).subscribe({
      next: account => {
        this.form = {
          accountGroupId: account.accountGroupId,
          code: account.code,
          name: account.name,
          parentId: account.parentId,
          allowPosting: account.allowPosting,
          isActive: account.isActive,
        };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load this account.');
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
