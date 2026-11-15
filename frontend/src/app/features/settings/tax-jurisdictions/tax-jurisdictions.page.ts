import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { Field } from '../../../shared/models/shared.models';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { TaxJurisdiction, TaxJurisdictionPayload } from './tax-jurisdiction.model';
import { TaxJurisdictionService } from './tax-jurisdiction.service';

@Component({
  selector: 'app-tax-jurisdictions-page',
  imports: [RouterLink, RecordFormComponent],
  templateUrl: './tax-jurisdictions.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class TaxJurisdictionsPageComponent implements OnInit, OnDestroy {
  readonly pageKey = 'tax-jurisdictions';
  readonly title = 'Tax jurisdictions';
  readonly description = 'Manage the jurisdictions available for tax codes and company registrations.';
  readonly indexPath = '/settings/tax-jurisdictions';
  readonly fields: Field[] = [
    { key: 'code', label: 'Jurisdiction code', required: true },
    { key: 'countryCode', label: 'Country code (ISO 3166-1 alpha-2)', required: true },
  ];
  rows: TaxJurisdiction[] = [];
  form: Record<string, unknown> = {};
  loading = false;
  saving = false;
  error = '';
  view: 'index' | 'create' | 'edit' = 'index';
  private currentRecordId: number | null = null;
  private routeSubscription?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly jurisdictions: TaxJurisdictionService,
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
        this.loading = false;
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

  setFormValue(key: string, value: unknown): void {
    this.form[key] = value;
  }

  submit(): void {
    const code = String(this.form['code'] ?? '').trim().toUpperCase();
    const countryCode = String(this.form['countryCode'] ?? '').trim().toUpperCase();
    if (!code || code.length > 40 || !/^[A-Z]{2}$/.test(countryCode)) {
      this.error = 'Enter a jurisdiction code (up to 40 characters) and a two-letter country code.';
      return;
    }
    if (this.view === 'edit' && this.currentRecordId === null) {
      this.error = 'A valid tax jurisdiction ID is required to update this record.';
      return;
    }

    const payload: TaxJurisdictionPayload = { code, countryCode };
    const request = this.view === 'edit' && this.currentRecordId !== null
      ? this.jurisdictions.update(this.currentRecordId, payload)
      : this.jurisdictions.create(payload);
    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save this tax jurisdiction.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  delete(jurisdiction: TaxJurisdiction): void {
    if (!window.confirm(`Delete jurisdiction ${jurisdiction.code} (${jurisdiction.countryCode})?`)) return;
    this.saving = true;
    this.error = '';
    this.jurisdictions.delete(jurisdiction.id).subscribe({
      next: () => {
        this.rows = this.rows.filter(row => row.id !== jurisdiction.id);
        this.saving = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not delete this tax jurisdiction.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  private loadRows(): void {
    this.loading = true;
    this.jurisdictions.getAll().subscribe({
      next: rows => {
        this.rows = rows;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load tax jurisdictions.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.error = 'A valid tax jurisdiction ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.jurisdictions.getById(this.currentRecordId).subscribe({
      next: jurisdiction => {
        this.form = { code: jurisdiction.code, countryCode: jurisdiction.countryCode };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load this tax jurisdiction.');
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
