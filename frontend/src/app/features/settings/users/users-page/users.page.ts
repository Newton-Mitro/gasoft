import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { Choice, Field } from '../../../../shared/models/shared.models';
import { RecordFormComponent } from '../../../../shared/components/record-form/record-form.component';
import { RecordPreviewComponent, shouldShowRecordPreview } from '../../../../shared/components/record-preview/record-preview.component';
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
import {
  FormValidationErrors,
  FormValidationService,
} from '../../../../shared/services/form-validation.service';

const ROLE_OPTIONS: Choice[] = [
  { label: 'Admin', value: 'Admin' },
  { label: 'Member', value: 'Member' },
  { label: 'Owner', value: 'Owner' },
];

@Component({
  selector: 'app-users-page',
  imports: [FormsModule, RouterLink, RecordFormComponent, RecordPreviewComponent, PaginationComponent, FieldValidationDirective, SearchableSelectDirective, InputComponent],
  templateUrl: './users.page.html',
  styleUrls: ['../../../../shared/styles/feature-page.css', './users.page.css'],
})
export class UsersPageComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  private readonly formValidation = inject(FormValidationService);
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

  get createPreviewFields(): Field[] {
    const fields: Field[] = [
      { key: 'email', label: 'Email address', type: 'email', required: true },
      { key: 'role', label: 'Workspace role', type: 'select', required: true, options: ROLE_OPTIONS },
    ];
    if (this.accountMode === 'new') {
      fields.unshift({ key: 'displayName', label: 'Full name', required: true });
      fields.push({ key: 'temporaryPassword', label: 'Temporary password', type: 'password', required: true });
    }
    return fields;
  }

  get showCreatePreview(): boolean {
    return this.view === 'create' && !this.saving &&
      shouldShowRecordPreview(this.pageKey, 'create', this.createPreviewFields);
  }

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
      this.formValidation.clear();
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
    const validationErrors = validateNewUser(displayName, email, role, temporaryPassword);
    this.formValidation.setErrors(validationErrors);
    if (Object.keys(validationErrors).length) {
      return;
    }
    this.error = '';
    const payload: CreateTenantUserPayload = { email, displayName, temporaryPassword, role };
    this.save(this.users.create(payload), 'Could not add this team member.');
  }

  setAccountMode(mode: 'new' | 'existing'): void {
    this.formValidation.clear();
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
    const validationErrors = validateExistingUser(email, role);
    this.formValidation.setErrors(validationErrors);
    if (Object.keys(validationErrors).length) {
      return;
    }

    this.error = '';
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
        this.showServerValidationErrors(error, failureMessage);
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
}

function toRecordId(params: ParamMap): number | null {
  const id = Number(params.get('recordId'));
  return Number.isSafeInteger(id) && id > 0 ? id : null;
}

function validateNewUser(
  displayName: string,
  email: string,
  role: string,
  temporaryPassword: string,
): FormValidationErrors {
  const errors: FormValidationErrors = {};
  if (!displayName) errors['displayName'] = ['Enter a full name.'];
  else if (displayName.length > 200) {
    errors['displayName'] = ['Full name must be 200 characters or fewer.'];
  }
  if (!isValidEmail(email)) errors['email'] = ['Enter a valid email address.'];
  if (!ROLE_OPTIONS.some(option => option.value === role)) {
    errors['role'] = ['Select a workspace role.'];
  }
  if (!temporaryPassword) errors['temporaryPassword'] = ['Enter a temporary password.'];
  else if (temporaryPassword.length > 128) {
    errors['temporaryPassword'] = ['Password must be 128 characters or fewer.'];
  }
  return errors;
}

function validateExistingUser(email: string, role: string): FormValidationErrors {
  const errors: FormValidationErrors = {};
  if (!isValidEmail(email)) errors['email'] = ['Enter a valid email address.'];
  if (!ROLE_OPTIONS.some(option => option.value === role)) {
    errors['role'] = ['Select a workspace role.'];
  }
  return errors;
}

function isValidEmail(email: string): boolean {
  return email.length <= 256 && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
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
