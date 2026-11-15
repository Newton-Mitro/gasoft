import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { Choice, Field, RecordFormMode } from '../../../shared/models/shared.models';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { Party, PartyPayload } from './party.model';
import { PartyService } from './party.service';

@Component({
  selector: 'app-parties-page',
  imports: [RouterLink, RecordFormComponent],
  templateUrl: './parties.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class PartiesPageComponent implements OnInit, OnDestroy {
  readonly pageKey = 'parties';
  readonly title = 'Customers & suppliers';
  readonly description = 'Maintain the customer and supplier directory for your tenant.';
  readonly fields: Field[] = [
    { key: 'partyNo', label: 'Party number', required: true },
    { key: 'displayName', label: 'Display name', required: true },
    { key: 'isCustomer', label: 'Customer', type: 'checkbox' },
    { key: 'isSupplier', label: 'Supplier', type: 'checkbox' },
    { key: 'email', label: 'Email', type: 'email' },
    { key: 'phone', label: 'Phone' },
    { key: 'defaultCurrencyCode', label: 'Default currency', type: 'select', optionSource: 'currencies' },
  ];
  readonly indexPath = '/parties';
  rows: Party[] = [];
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
    private readonly parties: PartyService,
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
        this.form = { isCustomer: false, isSupplier: false };
        this.loading = false;
        this.loadCurrencyOptions();
      } else if (this.view === 'edit') {
        this.loadCurrencyOptions();
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
    const payload: PartyPayload = {
      partyNo: String(this.form['partyNo'] ?? '').trim(),
      displayName: String(this.form['displayName'] ?? '').trim(),
      isCustomer: Boolean(this.form['isCustomer']),
      isSupplier: Boolean(this.form['isSupplier']),
      email: optionalTrimmedString(this.form['email']),
      phone: optionalTrimmedString(this.form['phone']),
      defaultCurrencyCode: optionalTrimmedString(this.form['defaultCurrencyCode']),
    };
    if (!payload.partyNo || !payload.displayName || (!payload.isCustomer && !payload.isSupplier)) {
      this.error = 'Enter a party number and display name, and choose customer, supplier, or both.';
      return;
    }

    let request;
    if (this.view === 'edit') {
      if (this.currentRecordId === null) {
        this.error = 'A valid party ID is required to update this record.';
        return;
      }
      request = this.parties.update(this.currentRecordId, payload);
    } else {
      request = this.parties.create(payload);
    }

    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save this customer or supplier.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  private loadCurrencyOptions(): void {
    this.optionData.getOptions('currencies').subscribe({
      next: options => {
        this.options = { ...this.options, currencies: options };
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load currency options.');
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRows(): void {
    this.loading = true;
    this.parties.getAll().subscribe({
      next: rows => {
        this.rows = rows;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load customers and suppliers.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.error = 'A valid party ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.parties.getById(this.currentRecordId).subscribe({
      next: party => {
        this.form = {
          partyNo: party.partyNo,
          displayName: party.displayName,
          isCustomer: party.isCustomer,
          isSupplier: party.isSupplier,
          email: party.email ?? '',
          phone: party.phone ?? '',
          defaultCurrencyCode: party.defaultCurrencyCode ?? '',
        };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load this customer or supplier.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}

function optionalTrimmedString(value: unknown): string | null {
  const text = String(value ?? '').trim();
  return text || null;
}

function toRecordId(params: ParamMap): number | null {
  const id = Number(params.get('recordId'));
  return Number.isSafeInteger(id) && id > 0 ? id : null;
}
