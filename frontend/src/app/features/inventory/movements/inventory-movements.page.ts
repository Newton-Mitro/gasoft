import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { PageChange, PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { Choice, Field, RecordFormMode } from '../../../shared/models/shared.models';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { InventoryIssuePayload, InventoryReceiptPayload } from '../data-access/inventory.models';
import { InventoryService } from '../data-access/inventory.service';
import { InventoryMovement } from './inventory-movement.model';
import { InventoryMovementService } from './inventory-movement.service';
import { NotificationService } from '../../../shared/services/notification.service';

const MOVEMENT_OPTIONS = ['items', 'warehouses', 'journals', 'periods', 'accounts'];

@Component({
  selector: 'app-inventory-movements-page',
  imports: [RouterLink, RecordFormComponent, PaginationComponent],
  templateUrl: './inventory-movements.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class InventoryMovementsPageComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  readonly pageKey = 'inventory-movements';
  readonly title = 'Stock movements';
  readonly description = 'Receive stock or issue stock with the matching journal and inventory accounts.';
  rows: InventoryMovement[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  fields: Field[] = [];
  form: Record<string, unknown> = {};
  options: Record<string, Choice[]> = {};
  loading = false;
  saving = false;
  error = '';
  view: 'index' | 'receive' | 'issue' = 'index';
  readonly indexPath = '/inventory/movements';
  private routeSubscription?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly movements: InventoryMovementService,
    private readonly inventory: InventoryService,
    private readonly optionData: OptionDataService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = combineLatest([this.route.data, this.route.paramMap]).subscribe(([data]) => {
      const view = data['view'];
      this.view = view === 'receive' || view === 'issue' ? view : 'index';
      this.error = '';
      if (this.view === 'index') this.loadRows();
      else this.openMovementForm(this.view);
    });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
  }

  get formMode(): RecordFormMode {
    return this.view === 'receive' || this.view === 'issue' ? this.view : 'create';
  }

  get movementTitle(): string {
    return this.view === 'receive' ? 'Record stock receipt' : 'Record stock issue';
  }

  setFormValue(key: string, value: unknown): void {
    this.form[key] = value;
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  onPageChange(change: PageChange): void {
    this.page = change.page;
    this.pageSize = change.pageSize;
    this.loadRows();
  }

  submit(): void {
    const receiving = this.view === 'receive';
    const base = {
      movementNumber: String(this.form['movementNumber'] ?? '').trim(),
      itemId: Number(this.form['itemId']),
      warehouseId: Number(this.form['warehouseId']),
      warehouseLocationId: null,
      journalId: Number(this.form['journalId']),
      accountingPeriodId: Number(this.form['accountingPeriodId']),
      inventoryAccountId: Number(this.form['inventoryAccountId']),
      movementDate: String(this.form['movementDate'] ?? ''),
      quantity: Number(this.form['quantity']),
    };
    const accountId = Number(this.form[receiving ? 'offsetAccountId' : 'costOfGoodsSoldAccountId']);
    const unitCost = Number(this.form['unitCost']);
    if (!base.movementNumber || !base.movementDate ||
      ![base.itemId, base.warehouseId, base.journalId, base.accountingPeriodId, base.inventoryAccountId, accountId]
        .every(isPositiveId) ||
      !Number.isFinite(base.quantity) || base.quantity <= 0 ||
      (receiving && (!Number.isFinite(unitCost) || unitCost < 0))) {
      this.error = 'Complete all required movement details with valid positive quantities and account selections.';
      return;
    }

    const request = receiving
      ? this.inventory.recordReceipt({
          ...base,
          offsetAccountId: accountId,
          unitCost,
        } satisfies InventoryReceiptPayload)
      : this.inventory.recordIssue({
          ...base,
          costOfGoodsSoldAccountId: accountId,
        } satisfies InventoryIssuePayload);
    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.saving = false;
        this.notifications.success('Inventory movement recorded successfully.');
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not record this stock movement.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  formatCost(value: number | string): string {
    const amount = Number(value);
    if (!Number.isFinite(amount)) return String(value);
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: 'USD',
      maximumFractionDigits: 2,
    }).format(amount);
  }

  private openMovementForm(mode: 'receive' | 'issue'): void {
    this.fields = [
      { key: 'movementNumber', label: mode === 'receive' ? 'Receipt number' : 'Issue number', required: true },
      { key: 'itemId', label: 'Item', type: 'select', required: true, optionSource: 'items' },
      { key: 'warehouseId', label: 'Warehouse', type: 'select', required: true, optionSource: 'warehouses' },
      { key: 'journalId', label: 'Journal', type: 'select', required: true, optionSource: 'journals' },
      { key: 'accountingPeriodId', label: 'Accounting period', type: 'select', required: true, optionSource: 'periods' },
      { key: 'inventoryAccountId', label: 'Inventory account', type: 'select', required: true, optionSource: 'accounts' },
      ...(mode === 'receive'
        ? [{ key: 'offsetAccountId', label: 'Offset account', type: 'select' as const, required: true, optionSource: 'accounts' as const }]
        : [{ key: 'costOfGoodsSoldAccountId', label: 'Cost of goods sold account', type: 'select' as const, required: true, optionSource: 'accounts' as const }]),
      { key: 'movementDate', label: 'Movement date', type: 'date', required: true },
      { key: 'quantity', label: 'Quantity', type: 'number', required: true },
      ...(mode === 'receive' ? [{ key: 'unitCost', label: 'Unit cost', type: 'number' as const, required: true }] : []),
    ];
    this.form = { quantity: 1 };
    if (mode === 'receive') this.form['unitCost'] = 0;
    this.loadOptions();
  }

  private loadRows(): void {
    this.loading = true;
    this.movements.getPage(this.page, this.pageSize).subscribe({
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
        this.notifications.error(errorMessage(error, 'Could not load stock movements.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadOptions(): void {
    this.options = {};
    for (const source of MOVEMENT_OPTIONS) {
      this.optionData.getOptions(source).subscribe({
        next: options => {
          this.options = { ...this.options, [source]: options };
          this.changeDetector.markForCheck();
        },
        error: error => {
          this.error = '';
          this.notifications.error(errorMessage(error, `Could not load ${source} options.`));
          this.changeDetector.markForCheck();
        },
      });
    }
  }
}

function isPositiveId(value: number): boolean {
  return Number.isSafeInteger(value) && value > 0;
}
