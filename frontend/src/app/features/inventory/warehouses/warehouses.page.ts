import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { PageChange, PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { Field, RecordFormMode } from '../../../shared/models/shared.models';
import { WarehouseLocationPayload } from '../inventory.models';
import { InventoryService } from '../inventory.service';
import { Warehouse, WarehousePayload } from './warehouse.model';
import { WarehouseService } from './warehouse.service';

@Component({
  selector: 'app-warehouses-page',
  imports: [RouterLink, RecordFormComponent, PaginationComponent],
  templateUrl: './warehouses.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class WarehousesPageComponent implements OnInit, OnDestroy {
  readonly pageKey = 'warehouses';
  readonly title = 'Warehouses';
  readonly description = 'Manage the company locations that hold stock.';
  readonly fields: Field[] = [
    { key: 'code', label: 'Warehouse code', required: true },
    { key: 'name', label: 'Warehouse name', required: true },
  ];
  readonly locationFields: Field[] = [
    { key: 'code', label: 'Location code', required: true },
    { key: 'name', label: 'Location name', required: true },
  ];
  readonly indexPath = '/inventory/warehouses';
  rows: Warehouse[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  form: Record<string, unknown> = {};
  loading = false;
  saving = false;
  error = '';
  view: 'index' | 'create' | 'edit' | 'location' = 'index';
  private routeSubscription?: Subscription;
  private currentRecordId: number | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly warehouses: WarehouseService,
    private readonly inventory: InventoryService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = combineLatest([this.route.data, this.route.paramMap]).subscribe(([data, params]) => {
      const view = data['view'];
      this.view = view === 'create' || view === 'edit' || view === 'location' ? view : 'index';
      this.currentRecordId = toRecordId(params);
      this.error = '';
      if (this.view === 'create' || this.view === 'location') {
        this.form = {};
        this.loading = false;
        if (this.view === 'location' && this.currentRecordId === null) {
          this.error = 'A valid warehouse ID is required to add a location.';
        }
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
    return this.view === 'edit' ? 'edit' : this.view === 'location' ? 'location' : 'create';
  }

  get activeFields(): Field[] {
    return this.view === 'location' ? this.locationFields : this.fields;
  }

  setFormValue(key: string, value: unknown): void {
    this.form[key] = value;
  }

  submit(): void {
    if (this.view === 'location') {
      this.submitLocation();
      return;
    }

    const payload: WarehousePayload = {
      code: String(this.form['code'] ?? '').trim(),
      name: String(this.form['name'] ?? '').trim(),
    };
    if (!payload.code || !payload.name) {
      this.error = 'Enter a warehouse code and name.';
      return;
    }

    let request;
    if (this.view === 'edit') {
      if (this.currentRecordId === null) {
        this.error = 'A valid warehouse ID is required to update this record.';
        return;
      }
      request = this.warehouses.update(this.currentRecordId, payload);
    } else {
      request = this.warehouses.create(payload);
    }

    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save this warehouse.');
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
    this.warehouses.getPage(this.page, this.pageSize).subscribe({
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
        this.error = errorMessage(error, 'Could not load warehouses.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.error = 'A valid warehouse ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.warehouses.getById(this.currentRecordId).subscribe({
      next: warehouse => {
        this.form = { code: warehouse.code, name: warehouse.name };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load this warehouse.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private submitLocation(): void {
    if (this.currentRecordId === null) {
      this.error = 'A valid warehouse ID is required to add a location.';
      return;
    }
    const payload: WarehouseLocationPayload = {
      code: String(this.form['code'] ?? '').trim(),
      name: String(this.form['name'] ?? '').trim(),
    };
    if (!payload.code || !payload.name) {
      this.error = 'Enter a location code and name.';
      return;
    }
    this.saving = true;
    this.error = '';
    this.inventory.createLocation(this.currentRecordId, payload).subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not create this warehouse location.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}

function toRecordId(params: ParamMap): number | null {
  const id = Number(params.get('recordId'));
  return Number.isSafeInteger(id) && id > 0 ? id : null;
}
