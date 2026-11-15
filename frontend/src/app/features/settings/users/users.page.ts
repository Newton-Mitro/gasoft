import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { Choice, Field } from '../../../shared/models/shared.models';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import {
  CreateTenantUserPayload,
  TenantUser,
  UpdateTenantUserPayload,
} from './user.model';
import { TenantUserService } from './user.service';

const ROLE_OPTIONS: Choice[] = [
  { label: 'Admin', value: 'Admin' },
  { label: 'Member', value: 'Member' },
  { label: 'Owner', value: 'Owner' },
];

@Component({
  selector: 'app-users-page',
  imports: [RouterLink, RecordFormComponent],
  templateUrl: './users.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class UsersPageComponent implements OnInit, OnDestroy {
  readonly pageKey = 'users';
  readonly title = 'Team members';
  readonly description = 'Manage tenant membership and access roles. Initial passwords must be shared privately.';
  readonly indexPath = '/users';
  readonly createFields: Field[] = [
    { key: 'displayName', label: 'Full name', required: true },
    { key: 'email', label: 'Email address', type: 'email', required: true },
    { key: 'role', label: 'Role', type: 'select', required: true, options: ROLE_OPTIONS },
    { key: 'temporaryPassword', label: 'Temporary password', type: 'password', required: true },
  ];
  readonly editFields: Field[] = [
    { key: 'role', label: 'Role', type: 'select', required: true, options: ROLE_OPTIONS },
    { key: 'isActive', label: 'Active membership', type: 'checkbox' },
  ];
  rows: TenantUser[] = [];
  fields: Field[] = this.createFields;
  form: Record<string, unknown> = {};
  loading = false;
  saving = false;
  error = '';
  success = '';
  view: 'index' | 'create' | 'edit' = 'index';
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
        this.fields = this.createFields;
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
        this.success = `${updated.displayName} ${updated.isActive ? 'reactivated' : 'deactivated'}.`;
        this.saving = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not update team member access.');
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

  private save(request: ReturnType<TenantUserService['create']>, failureMessage: string): void {
    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = errorMessage(error, failureMessage);
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRows(): void {
    this.loading = true;
    this.users.getAll().subscribe({
      next: rows => {
        this.rows = rows;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load team members.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
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
        this.error = errorMessage(error, 'Could not load this team member.');
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
