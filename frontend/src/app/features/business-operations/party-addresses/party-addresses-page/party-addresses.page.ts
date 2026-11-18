import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { RecordFormComponent } from '../../../../shared/components/record-form/record-form.component';
import { PageChange, PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { Choice, Field, RecordFormMode } from '../../../../shared/models/shared.models';
import { PartyAddress, PartyAddressPayload, PartyAddressType } from '../party-address.model';
import { PartyAddressService } from '../party-address.service';
import { NotificationService } from '../../../../shared/services/notification.service';

const ADDRESS_TYPES: Choice[] = [
  { label: 'Billing', value: 'Billing' },
  { label: 'Shipping', value: 'Shipping' },
  { label: 'Other', value: 'Other' },
];

@Component({
  selector: 'app-party-addresses-page',
  imports: [RouterLink, RecordFormComponent, PaginationComponent],
  templateUrl: './party-addresses.page.html',
  styleUrl: '../../../../shared/styles/feature-page.css',
})
export class PartyAddressesPageComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  readonly pageKey = 'party-addresses';
  readonly title = 'Party addresses';
  readonly description = 'Manage billing, shipping, and other addresses for this customer or supplier.';
  readonly fields: Field[] = [
    { key: 'addressType', label: 'Address type', type: 'select', required: true, options: ADDRESS_TYPES },
    { key: 'line1', label: 'Address line 1', required: true },
    { key: 'line2', label: 'Address line 2' },
    { key: 'city', label: 'City', required: true },
    { key: 'region', label: 'State / region' },
    { key: 'postalCode', label: 'Postal code' },
    { key: 'countryCode', label: 'Country code (2 letters)', required: true },
    { key: 'isDefault', label: 'Default address', type: 'checkbox' },
  ];
  rows: PartyAddress[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  form: Record<string, unknown> = {};
  loading = false;
  saving = false;
  error = '';
  view: 'index' | 'create' | 'edit' = 'index';
  partyId: number | null = null;
  private routeSubscription?: Subscription;
  private addressId: number | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly addresses: PartyAddressService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = combineLatest([this.route.data, this.route.paramMap]).subscribe(([data, params]) => {
      this.partyId = positiveId(params.get('partyId'));
      this.addressId = positiveId(params.get('recordId'));
      const view = data['view'];
      this.view = view === 'create' || view === 'edit' ? view : 'index';
      this.error = '';
      if (this.partyId === null) {
        this.error = 'A valid party ID is required to manage addresses.';
        return;
      }
      if (this.view === 'create') {
        this.form = { addressType: 'Billing', isDefault: false };
        this.loading = false;
      } else if (this.view === 'edit') {
        this.loadAddress();
      } else {
        this.loadRows();
      }
    });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
  }

  get indexPath(): string {
    return `/parties/${this.partyId}/addresses`;
  }

  get formMode(): RecordFormMode {
    return this.view === 'edit' ? 'edit' : 'create';
  }

  setFormValue(key: string, value: unknown): void {
    this.form[key] = value;
  }

  submit(): void {
    if (this.partyId === null) return;
    const addressType = this.form['addressType'];
    if (addressType !== 'Billing' && addressType !== 'Shipping' && addressType !== 'Other') {
      this.error = 'Choose a valid address type.';
      return;
    }
    const payload: PartyAddressPayload = {
      addressType: addressType as PartyAddressType,
      line1: textValue(this.form['line1']),
      line2: optionalTextValue(this.form['line2']),
      city: textValue(this.form['city']),
      region: optionalTextValue(this.form['region']),
      postalCode: optionalTextValue(this.form['postalCode']),
      countryCode: textValue(this.form['countryCode']).toUpperCase(),
      isDefault: Boolean(this.form['isDefault']),
    };
    if (!payload.line1 || !payload.city || !/^[A-Z]{2}$/.test(payload.countryCode)) {
      this.error = 'Enter address line 1, city, and a valid two-letter country code.';
      return;
    }

    let request;
    if (this.view === 'edit') {
      if (this.addressId === null) {
        this.error = 'A valid address ID is required to update this record.';
        return;
      }
      request = this.addresses.update(this.partyId, this.addressId, payload);
    } else {
      request = this.addresses.create(this.partyId, payload);
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
        this.notifications.error(errorMessage(error, 'Could not save this party address.'));
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
    if (this.partyId === null) return;
    this.loading = true;
    this.addresses.getPage(this.partyId, this.page, this.pageSize).subscribe({
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
        this.notifications.error(errorMessage(error, 'Could not load party addresses.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadAddress(): void {
    if (this.partyId === null || this.addressId === null) {
      this.error = 'A valid party and address ID are required to edit this record.';
      return;
    }
    this.loading = true;
    this.addresses.getById(this.partyId, this.addressId).subscribe({
      next: address => {
        this.form = {
          addressType: address.addressType,
          line1: address.line1,
          line2: address.line2 ?? '',
          city: address.city,
          region: address.region ?? '',
          postalCode: address.postalCode ?? '',
          countryCode: address.countryCode,
          isDefault: address.isDefault,
        };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load this party address.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}

function positiveId(value: string | null): number | null {
  const id = Number(value);
  return Number.isSafeInteger(id) && id > 0 ? id : null;
}

function textValue(value: unknown): string {
  return String(value ?? '').trim();
}

function optionalTextValue(value: unknown): string | null {
  return textValue(value) || null;
}
