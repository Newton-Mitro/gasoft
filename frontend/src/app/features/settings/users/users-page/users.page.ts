import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { Choice, Field } from '../../../../shared/models/shared.models';
import { RecordFormComponent } from '../../../../shared/components/record-form/record-form.component';
import { PageChange, PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import {
  AddExistingTenantUserPayload,
  CreateTenantUserPayload,
  TenantUser,
  UpdateTenantUserPayload,
} from '../user.model';
import { TenantUserService } from '../user.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { FieldValidationDirective } from '../../../../shared/directives/field-validation.directive';
import { SearchableSelectDirective } from '../../../../shared/directives/searchable-select.directive';
import { InputComponent } from '../../../../shared/components/input/input.component';

const ROLE_OPTIONS: Choice[] = [
  { label: 'Admin', value: 'Admin' },
  { label: 'Member', value: 'Member' },
  { label: 'Owner', value: 'Owner' },
];

@Component({
  selector: 'app-users-page',
  imports: [FormsModule, RouterLink, RecordFormComponent, PaginationComponent, FieldValidationDirective, SearchableSelectDirective, InputComponent],
  templateUrl: './users.page.html',
  styleUrls: ['../../../../shared/styles/feature-page.css', './users.page.css'],
})
export class UsersPageComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  readonly pageKey = 'users';
  readonly title = 'Team members';
  readonly description = 'Manage tenant membership and access roles. Add new accounts or invite an existing Gasoft user.';
  readonly indexPath = '/users';
  readonly roleOptions = ROLE_OPTIONS;
  readonly editFields: Field[] = [
    { key: 'role', label: 'Role', type: 'select', required: true, options: ROLE_OPTIONS },
    { key: 'isActive', label: 'Active membership', type: 'checkbox' },
  ];
  rows: TenantUser[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  fields: Field[] = [];
  form: Record<string, unknown> = {};
  loading = false;
  saving = false;
  error = '';
  success = '';
  view: 'index' | 'create' | 'edit' = 'index';
  accountMode: 'new' | 'existing' = 'new';
  private currentRecordId: number | null = null;
  private routeSubscription?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly users: TenantUserService,
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
        this.accountMode = 'new';
        this.form = {};
        this.loading = false;
      } else if (this.view === 'edit') {
        this.fields = this.editFields;
        this.loadRecord();
      } else {
        this.loadRows();
      }
    });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
  }

  setFormValue(key: string, value: unknown): void {
    this.form[key] = value;
  }

  submit(): void {
    if (this.view === 'edit') {
      this.submitAccessUpdate();
      return;
    }

    if (this.accountMode === 'existing') {
      this.submitExistingUser();
      return;
    }

    const email = String(this.form['email'] ?? '').trim();
    const displayName = String(this.form['displayName'] ?? '').trim();
    const temporaryPassword = String(this.form['temporaryPassword'] ?? '');
    const role = String(this.form['role'] ?? '');
    if (!email || email.length > 256 || !displayName || displayName.length > 200 ||
      !temporaryPassword || temporaryPassword.length > 128 ||
      !ROLE_OPTIONS.some(option => option.value === role)) {
      this.error = 'Enter a valid name, email, permitted role, and temporary password.';
      return;
    }
    const payload: CreateTenantUserPayload = { email, displayName, temporaryPassword, role };
    this.save(this.users.create(payload), 'Could not add this team member.');
  }

  setAccountMode(mode: 'new' | 'existing'): void {
    this.accountMode = mode;
    this.form = {};
    this.error = '';
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  toggleActive(user: TenantUser): void {
    const payload: UpdateTenantUserPayload = { role: user.role, isActive: !user.isActive };
    this.saving = true;
    this.error = '';
    this.success = '';
    this.users.update(user.id, payload).subscribe({
      next: updated => {
        this.rows = this.rows.map(row => row.id === updated.id ? updated : row);
        this.notifications.success(`${updated.displayName} ${updated.isActive ? 'reactivated' : 'deactivated'}.`);
        this.success = '';
        this.saving = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not update team member access.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private submitAccessUpdate(): void {
    if (this.currentRecordId === null) {
      this.error = 'A valid team member ID is required to update access.';
      return;
    }
    const role = String(this.form['role'] ?? '');
    if (!ROLE_OPTIONS.some(option => option.value === role)) {
      this.error = 'Choose a valid role.';
      return;
    }
    const payload: UpdateTenantUserPayload = {
      role,
      isActive: Boolean(this.form['isActive']),
    };
    this.save(this.users.update(this.currentRecordId, payload), 'Could not update team member access.');
  }

  private submitExistingUser(): void {
    const email = String(this.form['email'] ?? '').trim();
    const role = String(this.form['role'] ?? '');
    if (!email || email.length > 256 || !ROLE_OPTIONS.some(option => option.value === role)) {
      this.error = 'Enter a valid email address and permitted role.';
      return;
    }

    const payload: AddExistingTenantUserPayload = { email, role };
    this.save(this.users.addExisting(payload), 'Could not add this existing account.');
  }

  private save(request: ReturnType<TenantUserService['create']>, failureMessage: string): void {
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
        this.notifications.error(errorMessage(error, failureMessage));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRows(): void {
    this.loading = true;
    this.users.getPage(this.page, this.pageSize).subscribe({
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
        this.notifications.error(errorMessage(error, 'Could not load team members.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  onPageChange(change: PageChange): void {
    this.page = change.page;
    this.pageSize = change.pageSize;
    this.loadRows();
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.loading = false;
      this.error = 'A valid team member ID is required to edit access.';
      return;
    }
    this.loading = true;
    this.users.getById(this.currentRecordId).subscribe({
      next: user => {
        this.form = { role: user.role, isActive: user.isActive };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load this team member.'));
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
