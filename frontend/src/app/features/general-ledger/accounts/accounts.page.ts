import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { NgTemplateOutlet } from '@angular/common';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { Choice, Field, RecordFormMode } from '../../../shared/models/shared.models';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { Account, AccountPayload } from './account.model';
import { AccountService } from './account.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { accountDescendantIds, buildAccountTree, filterAccountTree } from './account-tree';

@Component({
  selector: 'app-accounts-page',
  imports: [FormsModule, RouterLink, NgTemplateOutlet, RecordFormComponent],
  templateUrl: './accounts.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class AccountsPageComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  readonly pageKey = 'accounts';
  readonly title = 'Chart of accounts';
  readonly description = 'Organize your ledger accounts and control where transactions can be posted.';
  readonly fields: Field[] = [
    { key: 'accountGroupId', label: 'Account group', type: 'select', required: true, optionSource: 'groups' },
    { key: 'code', label: 'Account code', required: true },
    { key: 'name', label: 'Account name', required: true },
    { key: 'parentId', label: 'Parent account', type: 'select' },
    { key: 'allowPosting', label: 'Allow postings', type: 'checkbox' },
  ];
  readonly indexPath = '/general-ledger/accounts';
  rows: Account[] = [];
  form: Record<string, unknown> = {};
  options: Record<string, Choice[]> = {};
  loading = false;
  saving = false;
  error = '';
  search = '';
  view: 'index' | 'create' | 'edit' = 'index';
  draggedAccountId: number | null = null;
  dropTargetAccountId: number | null = null;
  dropBlockedAccountId: number | null = null;
  dropTargetGroupId: number | null = null;
  dragStatus = '';
  reparentingAccountId: number | null = null;
  readonly collapsedGroupIds = new Set<number>();
  readonly collapsedAccountIds = new Set<number>();
  private routeSubscription?: Subscription;
  private currentRecordId: number | null = null;

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
  }

  get formMode(): RecordFormMode {
    return this.view === 'edit' ? 'edit' : 'create';
  }

  get formFields(): Field[] {
    const accountGroupId = Number(this.form['accountGroupId']);
    const excludedIds = this.currentRecordId === null
      ? new Set<number>()
      : accountDescendantIds(this.rows, this.currentRecordId).add(this.currentRecordId);
    const parentOptions = this.rows
      .filter(account => account.accountGroupId === accountGroupId &&
        !account.allowPosting &&
        !excludedIds.has(account.id))
      .map(account => ({ label: `${account.code} · ${account.name}`, value: account.id }));
    const fields = this.fields.map(field =>
      field.key === 'parentId'
        ? { ...field, options: parentOptions }
        : field);
    return this.view === 'edit'
      ? [...fields, { key: 'isActive', label: 'Active', type: 'checkbox' }]
      : fields;
  }

  get accountGroups(): { id: number; label: string; trees: ReturnType<typeof buildAccountTree> }[] {
    return this.options['groups']
      ?.map(group => {
        const id = Number(group.value);
        const trees = filterAccountTree(buildAccountTree(this.rows, id), this.search);
        return { id, label: group.label, trees };
      })
      .filter(group => !this.search.trim() || group.trees.length > 0) ?? [];
  }

  accountCountForGroup(groupId: number): number {
    return this.rows.filter(account => account.accountGroupId === groupId).length;
  }

  get groupCount(): number {
    return this.options['groups']?.length ?? 0;
  }

  get rootAccountCount(): number {
    return this.rows.filter(account => account.parentId === null).length;
  }

  get postableAccountCount(): number {
    return this.rows.filter(account => account.allowPosting).length;
  }

  expandAll(): void {
    this.collapsedGroupIds.clear();
    this.collapsedAccountIds.clear();
    this.changeDetector.markForCheck();
  }

  collapseAll(): void {
    this.options['groups']?.forEach(group => this.collapsedGroupIds.add(Number(group.value)));
    this.rows.forEach(account => {
      if (this.rows.some(child => child.parentId === account.id)) {
        this.collapsedAccountIds.add(account.id);
      }
    });
    this.changeDetector.markForCheck();
  }

  toggleGroup(groupId: number): void {
    this.toggleCollapsed(this.collapsedGroupIds, groupId);
  }

  isGroupCollapsed(groupId: number): boolean {
    return this.collapsedGroupIds.has(groupId);
  }

  toggleAccount(accountId: number): void {
    this.toggleCollapsed(this.collapsedAccountIds, accountId);
  }

  isAccountCollapsed(accountId: number): boolean {
    return this.collapsedAccountIds.has(accountId);
  }

  onSearchChange(value: string): void {
    this.search = value;
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
        this.notifications.success(`${this.title} ${this.view === 'edit' ? 'updated' : 'created'} successfully.`);
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not save this account.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  onDragStart(event: DragEvent, account: Account): void {
    if (this.reparentingAccountId !== null) {
      event.preventDefault();
      return;
    }
    this.draggedAccountId = account.id;
    this.dropBlockedAccountId = null;
    this.dragStatus = `Dragging ${account.code}. Drop it on an account in the same group to make it a child, or on a group heading to make it top-level. A parent account will no longer allow postings.`;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/plain', String(account.id));
    }
  }

  onDragEnd(): void {
    this.draggedAccountId = null;
    this.dropTargetAccountId = null;
    this.dropBlockedAccountId = null;
    this.dropTargetGroupId = null;
    this.dragStatus = '';
  }

  canDropOnAccount(target: Account): boolean {
    const dragged = this.rows.find(account => account.id === this.draggedAccountId);
    return !!dragged && !this.accountDropBlockReason(dragged, target);
  }

  private accountDropBlockReason(dragged: Account, target: Account): string {
    if (dragged.id === target.id) return 'An account cannot be its own parent.';
    if (dragged.accountGroupId !== target.accountGroupId) {
      return 'To change account groups, drop the account on the destination group heading.';
    }
    if (accountDescendantIds(this.rows, dragged.id).has(target.id)) {
      return 'An account cannot be moved beneath one of its descendants.';
    }
    return '';
  }

  onAccountDragOver(event: DragEvent, target: Account): void {
    const dragged = this.rows.find(account => account.id === this.draggedAccountId);
    if (!dragged) return;
    const blockedReason = this.accountDropBlockReason(dragged, target);
    if (blockedReason) {
      this.dropTargetAccountId = null;
      this.dropBlockedAccountId = target.id;
      this.dragStatus = blockedReason;
      return;
    }
    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
    this.dropTargetAccountId = target.id;
    this.dropBlockedAccountId = null;
    this.dropTargetGroupId = null;
    this.dragStatus = target.allowPosting
      ? `Drop to make ${dragged.code} a child of ${target.code}. Allow postings will be turned off for ${target.code}.`
      : `Drop to make ${dragged.code} a child of ${target.code}.`;
  }

  onAccountDrop(event: DragEvent, target: Account): void {
    event.preventDefault();
    event.stopPropagation();
    if (!this.canDropOnAccount(target)) return;
    this.moveAccount(this.draggedAccountId!, target.id, target.accountGroupId);
  }

  canDropToGroupRoot(groupId: number): boolean {
    const dragged = this.rows.find(account => account.id === this.draggedAccountId);
    return !!dragged &&
      (dragged.accountGroupId !== groupId || dragged.parentId !== null);
  }

  onGroupDragOver(event: DragEvent, groupId: number): void {
    if (!this.canDropToGroupRoot(groupId)) return;
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
    this.dropTargetGroupId = groupId;
    this.dropTargetAccountId = null;
    const dragged = this.rows.find(account => account.id === this.draggedAccountId);
    const group = this.options['groups']?.find(option => Number(option.value) === groupId);
    if (dragged && group) this.dragStatus = `Drop to move ${dragged.code} to ${group.label} as a top-level account.`;
  }

  onGroupDrop(event: DragEvent, groupId: number): void {
    event.preventDefault();
    if (!this.canDropToGroupRoot(groupId)) return;
    this.moveAccount(this.draggedAccountId!, null, groupId);
  }

  private moveAccount(accountId: number, parentId: number | null, accountGroupId: number): void {
    const account = this.rows.find(item => item.id === accountId);
    if (!account || (account.parentId === parentId && account.accountGroupId === accountGroupId)) {
      this.onDragEnd();
      return;
    }
    this.reparentingAccountId = accountId;
    this.error = '';
    const newParent = parentId === null ? undefined : this.rows.find(item => item.id === parentId);
    this.accounts.update(account.id, {
      accountGroupId,
      code: account.code,
      name: account.name,
      parentId,
      allowPosting: account.allowPosting,
      isActive: account.isActive,
      disableParentPosting: newParent?.allowPosting ?? false,
    }).subscribe({
      next: updated => {
        if (updated.parentId !== parentId || updated.accountGroupId !== accountGroupId) {
          this.notifications.error('The server did not save the requested account parent.');
          this.reparentingAccountId = null;
          this.onDragEnd();
          this.changeDetector.markForCheck();
          return;
        }
        this.accounts.getAll().subscribe({
          next: rows => {
            this.rows = rows;
            const groupChanged = accountGroupId !== account.accountGroupId;
            const message = groupChanged
              ? `${updated.code} and its child accounts moved to another account group.`
              : `${updated.code} is now ${parentId === null ? 'a top-level account' : 'a child account'}.`;
            this.notifications.success(message);
            this.reparentingAccountId = null;
            this.onDragEnd();
            this.changeDetector.markForCheck();
          },
          error: error => {
            this.notifications.error(errorMessage(error, 'The account moved, but the account tree could not be refreshed.'));
            this.reparentingAccountId = null;
            this.onDragEnd();
            this.changeDetector.markForCheck();
          },
        });
      },
      error: error => {
        this.notifications.error(errorMessage(error, 'Could not change the account parent.'));
        this.reparentingAccountId = null;
        this.onDragEnd();
        this.changeDetector.markForCheck();
      },
    });
  }

  private toggleCollapsed(collapsedIds: Set<number>, id: number): void {
    if (collapsedIds.has(id)) collapsedIds.delete(id);
    else collapsedIds.add(id);
    this.changeDetector.markForCheck();
  }

  private loadGroupOptions(): void {
    combineLatest([
      this.optionData.getOptions('groups'),
      this.accounts.getAll(),
    ]).subscribe({
      next: ([options, accounts]) => {
        this.options = { ...this.options, groups: options };
        this.rows = accounts;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load account group options.'));
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRows(): void {
    this.loading = true;
    combineLatest([
      this.accounts.getAll(),
      this.optionData.getOptions('groups'),
    ]).subscribe({
      next: ([rows, groups]) => {
        this.rows = rows;
        this.options = { ...this.options, groups };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load accounts.'));
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
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load this account.'));
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
