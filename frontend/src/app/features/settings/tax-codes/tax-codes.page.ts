import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, map, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { Choice, Field } from '../../../shared/models/shared.models';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { PageChange, PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { OptionDataCacheService } from '../../../shared/data/option-data-cache.service';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { TaxCode, TaxCodePayload, TaxRate, TaxRatePayload } from './tax-code.model';
import { TaxCodeService } from './tax-code.service';

const TAX_CODE_OPTIONS = ['jurisdictions', 'taxCategories'];

@Component({
  selector: 'app-tax-codes-page',
  imports: [FormsModule, RouterLink, RecordFormComponent, PaginationComponent],
  templateUrl: './tax-codes.page.html',
  styleUrls: ['../../../shared/styles/feature-page.css', './tax-codes.page.css'],
})
export class TaxCodesPageComponent implements OnInit, OnDestroy {
  readonly pageKey = 'tax-codes';
  readonly title = 'Tax codes and rates';
  readonly description = 'Configure company-specific tax codes and effective-dated rates. Confirm tax treatment and rates with an authoritative source.';
  readonly indexPath = '/settings/tax-codes';
  readonly fields: Field[] = [
    { key: 'jurisdictionId', label: 'Jurisdiction', type: 'select', required: true, optionSource: 'jurisdictions' },
    { key: 'categoryId', label: 'Category', type: 'select', required: true, optionSource: 'taxCategories' },
    { key: 'code', label: 'Tax code', required: true },
    { key: 'taxType', label: 'Tax type', required: true },
  ];
  rows: TaxCode[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  options: Record<string, Choice[]> = {};
  optionsLoading = [...TAX_CODE_OPTIONS];
  optionsFailed: string[] = [];
  form: Record<string, unknown> = {};
  ratePercent: number | null = null;
  validFrom = '';
  validTo = '';
  loading = false;
  saving = false;
  formReady = false;
  error = '';
  rateError = '';
  view: 'index' | 'create' | 'edit' = 'index';
  activeTaxCodeId: number | null = null;
  editingRateId: number | null = null;
  private currentRecordId: number | null = null;
  private routeSubscription?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly taxCodes: TaxCodeService,
    private readonly optionData: OptionDataService,
    private readonly optionCache: OptionDataCacheService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadOptions();
    this.routeSubscription = combineLatest([this.route.data, this.route.paramMap]).subscribe(([data, params]) => {
      const view = data['view'];
      this.view = view === 'create' || view === 'edit' ? view : 'index';
      this.currentRecordId = toRecordId(params);
      this.error = '';
      this.formReady = false;
      if (this.view === 'create') {
        this.form = {};
        this.formReady = true;
      } else if (this.view === 'edit') {
        this.loadRows(true);
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

  submitCode(): void {
    const jurisdictionId = Number(this.form['jurisdictionId']);
    const categoryId = Number(this.form['categoryId']);
    const code = String(this.form['code'] ?? '').trim().toUpperCase();
    const taxType = String(this.form['taxType'] ?? '').trim();
    if (!isPositiveId(jurisdictionId) || !isPositiveId(categoryId) ||
        !code || code.length > 40 || !taxType || taxType.length > 40) {
      this.error = 'Choose a jurisdiction and category, then enter a tax code and type (up to 40 characters each).';
      return;
    }
    if (this.view === 'edit' && this.currentRecordId === null) {
      this.error = 'A valid tax code ID is required to update this record.';
      return;
    }

    const payload: TaxCodePayload = { jurisdictionId, categoryId, code, taxType };
    const request = this.view === 'edit' && this.currentRecordId !== null
      ? this.taxCodes.update(this.currentRecordId, payload)
      : this.taxCodes.create(payload);
    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.saving = false;
        this.optionCache.clear();
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save this tax code.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  deleteCode(code: TaxCode): void {
    if (!window.confirm(`Delete tax code ${code.code} and its rate history?`)) return;
    this.saving = true;
    this.error = '';
    this.taxCodes.delete(code.id).subscribe({
      next: () => {
        this.rows = this.rows.filter(row => row.id !== code.id);
        this.activeTaxCodeId = this.activeTaxCodeId === code.id ? null : this.activeTaxCodeId;
        this.saving = false;
        this.optionCache.clear();
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not delete this tax code.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  toggleRates(code: TaxCode): void {
    this.activeTaxCodeId = this.activeTaxCodeId === code.id ? null : code.id;
    this.rateError = '';
    this.editingRateId = null;
    this.resetRateForm();
  }

  editRate(rate: TaxRate): void {
    this.editingRateId = rate.id;
    this.ratePercent = rate.ratePercent;
    this.validFrom = rate.validFrom;
    this.validTo = rate.validTo ?? '';
    this.rateError = '';
  }

  beginRateEdit(): void {
    this.resetRateForm();
    this.rateError = '';
  }

  saveRate(code: TaxCode): void {
    if (this.ratePercent === null || !Number.isFinite(this.ratePercent) ||
        this.ratePercent < 0 || this.ratePercent > 100 ||
        Math.round(this.ratePercent * 1_000_000) !== this.ratePercent * 1_000_000 ||
        !isIsoDate(this.validFrom) || (this.validTo && !isIsoDate(this.validTo)) ||
        (this.validTo && this.validTo < this.validFrom)) {
      this.rateError = 'Enter a rate from 0 to 100 (up to 6 decimal places) and a valid date range.';
      return;
    }
    const payload: TaxRatePayload = {
      ratePercent: this.ratePercent,
      validFrom: this.validFrom,
      validTo: this.validTo || null,
    };
    const request = this.editingRateId === null
      ? this.taxCodes.createRate(code.id, payload)
      : this.taxCodes.updateRate(code.id, this.editingRateId, payload);
    this.saving = true;
    this.rateError = '';
    request.subscribe({
      next: () => {
        this.saving = false;
        this.optionCache.clear();
        this.loadRows();
        this.activeTaxCodeId = code.id;
        this.resetRateForm();
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.rateError = errorMessage(error, 'Could not save this tax rate.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  deleteRate(code: TaxCode, rate: TaxRate): void {
    if (!window.confirm(`Delete the ${rate.ratePercent}% rate effective ${rate.validFrom}?`)) return;
    this.saving = true;
    this.rateError = '';
    this.taxCodes.deleteRate(code.id, rate.id).subscribe({
      next: () => {
        this.saving = false;
        this.optionCache.clear();
        this.loadRows();
        this.activeTaxCodeId = code.id;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.rateError = errorMessage(error, 'Could not delete this tax rate.');
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
    this.activeTaxCodeId = null;
    this.loadRows();
  }

  private loadRows(loadRecord = false): void {
    this.loading = true;
    const request = loadRecord
      ? this.taxCodes.getAll().pipe(map(items => ({
          items,
          page: 1,
          pageSize: items.length,
          totalCount: items.length,
          totalPages: items.length ? 1 : 0,
        })))
      : this.taxCodes.getPage(this.page, this.pageSize);
    request.subscribe({
      next: result => {
        this.rows = result.items;
        this.page = result.page;
        this.pageSize = result.pageSize;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.loading = false;
        if (loadRecord) {
          const code = this.currentRecordId === null
            ? undefined
            : result.items.find(row => row.id === this.currentRecordId);
          if (!code) {
            this.error = 'Tax code was not found in this company.';
          } else {
            this.form = {
              jurisdictionId: code.jurisdictionId,
              categoryId: code.categoryId,
              code: code.code,
              taxType: code.taxType,
            };
            this.formReady = true;
          }
        }
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load tax codes.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadOptions(): void {
    this.options = {};
    this.optionsLoading = [...TAX_CODE_OPTIONS];
    this.optionsFailed = [];
    for (const source of TAX_CODE_OPTIONS) {
      this.optionData.getOptions(source).subscribe({
        next: options => {
          this.options[source] = options;
          this.optionsLoading = this.optionsLoading.filter(item => item !== source);
          this.changeDetector.markForCheck();
        },
        error: error => {
          this.optionsFailed = [...this.optionsFailed, source];
          this.optionsLoading = this.optionsLoading.filter(item => item !== source);
          this.error = errorMessage(error, `Could not load ${source} options.`);
          this.changeDetector.markForCheck();
        },
      });
    }
  }

  private resetRateForm(): void {
    this.editingRateId = null;
    this.ratePercent = null;
    this.validFrom = new Date().toISOString().slice(0, 10);
    this.validTo = '';
  }
}

function toRecordId(params: ParamMap): number | null {
  const id = Number(params.get('recordId'));
  return isPositiveId(id) ? id : null;
}

function isPositiveId(value: number): boolean {
  return Number.isSafeInteger(value) && value > 0;
}

function isIsoDate(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const [year, month, day] = value.split('-').map(Number);
  const date = new Date(Date.UTC(year, month - 1, day));
  return date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day;
}
