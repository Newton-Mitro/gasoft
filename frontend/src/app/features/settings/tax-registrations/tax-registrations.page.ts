import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { Choice, Field } from '../../../shared/models/shared.models';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { TaxRegistration, TaxRegistrationPayload } from './tax-registration.model';
import { TaxRegistrationService } from './tax-registration.service';

const TAX_REGISTRATION_OPTIONS = ['jurisdictions'];

@Component({
  selector: 'app-tax-registrations-page',
  imports: [RouterLink, RecordFormComponent],
  templateUrl: './tax-registrations.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class TaxRegistrationsPageComponent implements OnInit, OnDestroy {
  readonly pageKey = 'tax-registrations';
  readonly title = 'Tax registrations';
  readonly description = 'Review configured tax references and register your company in a jurisdiction.';
  readonly indexPath = '/settings/tax-registrations';
  readonly fields: Field[] = [
    { key: 'jurisdictionId', label: 'Tax jurisdiction', type: 'select', required: true, optionSource: 'jurisdictions' },
    { key: 'registrationNumber', label: 'Registration number', required: true },
  ];
  rows: TaxRegistration[] = [];
  taxReferenceRows: Record<string, unknown>[] = [];
  options: Record<string, Choice[]> = {};
  form: Record<string, unknown> = {};
  loading = false;
  formReady = false;
  saving = false;
  error = '';
  view: 'index' | 'create' | 'edit' = 'index';
  private currentRecordId: number | null = null;
  private routeSubscription?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly registrations: TaxRegistrationService,
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
        this.formReady = true;
        this.loading = false;
        this.loadOptions();
      } else if (this.view === 'edit') {
        this.formReady = false;
        this.loadOptions();
        this.loadRecord();
      } else {
        this.loadRows();
        this.loadTaxReference();
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
    const jurisdictionId = Number(this.form['jurisdictionId']);
    const registrationNumber = String(this.form['registrationNumber'] ?? '').trim();
    if (!isPositiveId(jurisdictionId) || !registrationNumber || registrationNumber.length > 100) {
      this.error = 'Choose a tax jurisdiction and enter a registration number of at most 100 characters.';
      return;
    }
    if (this.view === 'edit' && this.currentRecordId === null) {
      this.error = 'A valid tax registration ID is required to update this record.';
      return;
    }
    const payload: TaxRegistrationPayload = { jurisdictionId, registrationNumber };
    const request = this.view === 'edit' && this.currentRecordId !== null
      ? this.registrations.update(this.currentRecordId, payload)
      : this.registrations.create(payload);
    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save this tax registration.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  displayValue(value: unknown): string {
    if (value === null || value === undefined || value === '') return '—';
    return String(value);
  }

  private loadRows(): void {
    this.loading = true;
    this.registrations.getAll().subscribe({
      next: rows => {
        this.rows = rows;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load tax registrations.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.loading = false;
      this.formReady = false;
      this.error = 'A valid tax registration ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.registrations.getById(this.currentRecordId).subscribe({
      next: registration => {
        this.form = {
          jurisdictionId: registration.jurisdictionId,
          registrationNumber: registration.registrationNumber,
        };
        this.formReady = true;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load this tax registration.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadOptions(): void {
    this.options = {};
    for (const source of TAX_REGISTRATION_OPTIONS) {
      this.optionData.getOptions(source).subscribe({
        next: options => {
          this.options[source] = options;
          this.changeDetector.markForCheck();
        },
        error: error => {
          this.error = errorMessage(error, `Could not load ${source} options.`);
          this.changeDetector.markForCheck();
        },
      });
    }
  }

  private loadTaxReference(): void {
    this.optionData.getTaxReferenceRows().subscribe({
      next: rows => {
        this.taxReferenceRows = rows;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load tax reference data.');
        this.changeDetector.markForCheck();
      },
    });
  }
}

function toRecordId(params: ParamMap): number | null {
  const id = Number(params.get('recordId'));
  return isPositiveId(id) ? id : null;
}

function isPositiveId(value: number): boolean {
  return Number.isSafeInteger(value) && value > 0;
}
