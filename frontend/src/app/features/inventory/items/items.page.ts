import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { Choice, Field, RecordFormMode } from '../../../shared/models/shared.models';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { CostingMethod, Item, ItemPayload } from './item.model';
import { ItemService } from './item.service';

const COSTING_METHODS: Choice[] = ['FIFO', 'AVCO'].map(value => ({ label: value, value }));

@Component({
  selector: 'app-items-page',
  imports: [RouterLink, RecordFormComponent],
  templateUrl: './items.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class ItemsPageComponent implements OnInit, OnDestroy {
  readonly pageKey = 'items';
  readonly title = 'Inventory items';
  readonly description = 'Maintain products and services tracked in the inventory ledger.';
  readonly fields: Field[] = [
    { key: 'categoryId', label: 'Category', type: 'select', required: true, optionSource: 'categories' },
    { key: 'sku', label: 'SKU', required: true },
    { key: 'name', label: 'Item name', required: true },
    { key: 'baseUnitCode', label: 'Base unit', type: 'select', required: true, optionSource: 'units' },
    { key: 'costingMethod', label: 'Costing method', type: 'select', required: true, options: COSTING_METHODS },
  ];
  readonly indexPath = '/inventory/items';
  rows: Item[] = [];
  form: Record<string, unknown> = {};
  options: Record<string, Choice[]> = {};
  loading = false;
  saving = false;
  error = '';
  view: 'index' | 'create' | 'edit' = 'index';
  private routeSubscription?: Subscription;
  private currentRecordId: number | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly items: ItemService,
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
        this.form = {};
        this.loadOptions();
      } else if (this.view === 'edit') {
        this.loadOptions();
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
    const costingMethod = this.form['costingMethod'];
    if (costingMethod !== 'FIFO' && costingMethod !== 'AVCO') {
      this.error = 'Choose a valid costing method.';
      return;
    }
    const payload: ItemPayload = {
      categoryId: Number(this.form['categoryId']),
      sku: String(this.form['sku'] ?? '').trim(),
      name: String(this.form['name'] ?? '').trim(),
      baseUnitCode: String(this.form['baseUnitCode'] ?? ''),
      costingMethod: costingMethod as CostingMethod,
    };
    if (!Number.isSafeInteger(payload.categoryId) || payload.categoryId < 1) {
      this.error = 'Choose a valid item category.';
      return;
    }

    let request;
    if (this.view === 'edit') {
      if (this.currentRecordId === null) {
        this.error = 'A valid item ID is required to update this record.';
        return;
      }
      request = this.items.update(this.currentRecordId, payload);
    } else {
      request = this.items.create(payload);
    }

    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save this inventory item.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  private loadOptions(): void {
    this.options = {};
    for (const source of ['categories', 'units']) {
      this.optionData.getOptions(source).subscribe({
        next: options => {
          this.options = { ...this.options, [source]: options };
          this.changeDetector.markForCheck();
        },
        error: error => {
          this.error = errorMessage(error, `Could not load ${source} options.`);
          this.changeDetector.markForCheck();
        },
      });
    }
  }

  private loadRows(): void {
    this.loading = true;
    this.items.getAll().subscribe({
      next: rows => {
        this.rows = rows;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load inventory items.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.error = 'A valid item ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.items.getById(this.currentRecordId).subscribe({
      next: item => {
        this.form = {
          categoryId: item.categoryId,
          sku: item.sku,
          name: item.name,
          baseUnitCode: item.baseUnitCode,
          costingMethod: item.costingMethod,
        };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load this inventory item.');
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
