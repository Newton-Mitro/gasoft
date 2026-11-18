import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { PageChange, PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { Choice, Field, RecordFormMode } from '../../../shared/models/shared.models';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { InventoryCountPostingPayload } from '../data-access/inventory.models';
import { InventoryService } from '../data-access/inventory.service';
import { InventoryCount, InventoryCountItem, InventoryCountPayload } from './inventory-count.model';
import { InventoryCountService } from './inventory-count.service';
import { NotificationService } from '../../../shared/services/notification.service';

@Component({
  selector: 'app-inventory-counts-page',
  imports: [RouterLink, RecordFormComponent, PaginationComponent],
  templateUrl: './inventory-counts.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class InventoryCountsPageComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  readonly pageKey = 'inventory-counts';
  readonly title = 'Inventory counts';
  readonly description = 'Capture a physical stock count, compare it with system quantities, and post adjustments.';
  readonly fields: Field[] = [
    { key: 'warehouseId', label: 'Warehouse', type: 'select', required: true, optionSource: 'warehouses' },
    { key: 'countNumber', label: 'Count number', required: true },
    { key: 'countDate', label: 'Count date', type: 'date', required: true },
    { key: 'itemId', label: 'Item', type: 'select', required: true, optionSource: 'items' },
    { key: 'countedQuantity', label: 'Counted quantity', type: 'number', required: true },
    { key: 'adjustmentUnitCost', label: 'Adjustment unit cost (optional)', type: 'number' },
  ];
  readonly postingFields: Field[] = [
    { key: 'journalId', label: 'Journal', type: 'select', required: true, optionSource: 'journals' },
    { key: 'accountingPeriodId', label: 'Accounting period', type: 'select', required: true, optionSource: 'periods' },
    { key: 'inventoryAccountId', label: 'Inventory account', type: 'select', required: true, optionSource: 'accounts' },
    { key: 'adjustmentAccountId', label: 'Adjustment expense account', type: 'select', required: true, optionSource: 'accounts' },
  ];
  readonly indexPath = '/inventory/counts';
  rows: InventoryCount[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  form: Record<string, unknown> = {};
  options: Record<string, Choice[]> = {};
  loading = false;
  saving = false;
  error = '';
  view: 'index' | 'create' | 'edit' = 'index';
  postingCountId: number | null = null;
  private routeSubscription?: Subscription;
  private currentRecordId: number | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly counts: InventoryCountService,
    private readonly inventory: InventoryService,
    private readonly optionData: OptionDataService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = combineLatest([this.route.data, this.route.paramMap]).subscribe(([data, params]) => {
      const view = data['view'];
      this.view = view === 'create' || view === 'edit' ? view : 'index';
      this.currentRecordId = toRecordId(params);
      this.error = '';
      this.postingCountId = null;
      if (this.view === 'create') {
        this.form = {};
        this.loading = false;
        this.loadOptions(['warehouses', 'items']);
      } else if (this.view === 'edit') {
        this.loadOptions(['warehouses', 'items']);
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
    const warehouseId = Number(this.form['warehouseId']);
    const itemId = Number(this.form['itemId']);
    const countedQuantity = Number(this.form['countedQuantity']);
    const adjustmentCostValue = this.form['adjustmentUnitCost'];
    const adjustmentUnitCost = adjustmentCostValue === '' || adjustmentCostValue === null ||
      adjustmentCostValue === undefined ? null : Number(adjustmentCostValue);
    if (!isPositiveId(warehouseId) || !isPositiveId(itemId) || !Number.isFinite(countedQuantity) ||
      countedQuantity < 0 || (adjustmentUnitCost !== null && (!Number.isFinite(adjustmentUnitCost) || adjustmentUnitCost < 0))) {
      this.error = 'Enter a valid warehouse, item, counted quantity, and optional adjustment cost.';
      return;
    }

    const item: InventoryCountItem = { itemId, countedQuantity, adjustmentUnitCost };
    const payload: InventoryCountPayload = {
      warehouseId,
      countNumber: String(this.form['countNumber'] ?? '').trim(),
      countDate: String(this.form['countDate'] ?? ''),
      items: [item],
    };
    if (!payload.countNumber || !payload.countDate) {
      this.error = 'Enter a count number and count date.';
      return;
    }

    let request;
    if (this.view === 'edit') {
      if (this.currentRecordId === null) {
        this.error = 'A valid inventory count ID is required to update this record.';
        return;
      }
      request = this.counts.update(this.currentRecordId, payload);
    } else {
      request = this.counts.create(payload);
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
        this.notifications.error(errorMessage(error, 'Could not save this inventory count.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    if (this.view !== 'index') {
      void this.router.navigateByUrl(this.indexPath);
      return;
    }
    this.postingCountId = null;
    this.error = '';
  }

  onPageChange(change: PageChange): void {
    this.page = change.page;
    this.pageSize = change.pageSize;
    this.loadRows();
  }

  openPostForm(count: InventoryCount): void {
    if (count.status !== 'Draft') return;
    this.postingCountId = count.id;
    this.form = {};
    this.error = '';
    this.loadOptions(['journals', 'periods', 'accounts']);
  }

  submitPost(): void {
    if (this.postingCountId === null) return;
    const payload: InventoryCountPostingPayload = {
      journalId: Number(this.form['journalId']),
      accountingPeriodId: Number(this.form['accountingPeriodId']),
      inventoryAccountId: Number(this.form['inventoryAccountId']),
      adjustmentAccountId: Number(this.form['adjustmentAccountId']),
    };
    if (![payload.journalId, payload.accountingPeriodId, payload.inventoryAccountId, payload.adjustmentAccountId]
      .every(isPositiveId)) {
      this.error = 'Choose a journal, accounting period, inventory account, and adjustment account.';
      return;
    }
    this.saving = true;
    this.error = '';
    this.inventory.postCount(this.postingCountId, payload).subscribe({
      next: () => {
        this.saving = false;
        this.postingCountId = null;
        this.notifications.success('Inventory count posted and adjusted successfully.');
        this.loadRows();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not post this inventory count.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadOptions(sources: string[]): void {
    this.options = {};
    for (const source of sources) {
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

  private loadRows(): void {
    this.loading = true;
    this.counts.getPage(this.page, this.pageSize).subscribe({
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
        this.notifications.error(errorMessage(error, 'Could not load inventory counts.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.error = 'A valid inventory count ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.counts.getById(this.currentRecordId).subscribe({
      next: count => {
        const item = count.items.length === 1 ? count.items[0] : null;
        if (count.status !== 'Draft' || !item) {
          this.error = count.status !== 'Draft'
            ? 'Only draft inventory counts can be edited.'
            : 'Only single-item draft counts can be edited in this form.';
          this.loading = false;
          this.changeDetector.markForCheck();
          return;
        }
        this.form = {
          warehouseId: count.warehouseId,
          countNumber: count.countNumber,
          countDate: count.countDate,
          itemId: item.itemId,
          countedQuantity: item.countedQuantity,
          adjustmentUnitCost: item.adjustmentUnitCost,
        };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load this inventory count.'));
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

function isPositiveId(value: number): boolean {
  return Number.isSafeInteger(value) && value > 0;
}
