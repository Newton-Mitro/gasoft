import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { errorMessage } from '../../../shared/utils/page-utils';
import { Choice, Field } from '../../../shared/models/shared.models';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { Company, UpdateCompanyPayload } from './company.model';
import { CompanyService } from './company.service';
import { AuthenticatedImageComponent } from '../../../shared/components/authenticated-image/authenticated-image.component';

@Component({
  selector: 'app-company-settings-page',
  imports: [RouterLink, RecordFormComponent, AuthenticatedImageComponent],
  templateUrl: './company-settings.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class CompanySettingsPageComponent implements OnInit, OnDestroy {
  readonly pageKey = 'company';
  readonly title = 'Company settings';
  readonly description = 'Manage your organization identity, contact details, tax information, and functional currency.';
  readonly fields: Field[] = [
    { key: 'legalName', label: 'Legal name', required: true },
    { key: 'functionalCurrencyCode', label: 'Functional currency', type: 'select', required: true, optionSource: 'currencies' },
    { key: 'phone', label: 'Phone', type: 'tel' },
    { key: 'email', label: 'Contact email', type: 'email' },
    { key: 'website', label: 'Website', type: 'url' },
    { key: 'address', label: 'Street address' },
    { key: 'cityRegion', label: 'City and region/state' },
    { key: 'postalCode', label: 'Postal/ZIP code' },
    { key: 'country', label: 'Country' },
    { key: 'taxId', label: 'Tax/VAT registration number' },
  ];
  company: Company | null = null;
  form: Record<string, unknown> = {};
  options: Record<string, Choice[]> = {};
  editing = false;
  loading = false;
  saving = false;
  logoUploading = false;
  logoError = '';
  logoPreview: string | null = null;
  selectedLogo: File | null = null;
  private logoObjectUrl: string | null = null;
  error = '';
  private routeSubscription?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly auth: AuthService,
    private readonly companies: CompanyService,
    private readonly optionData: OptionDataService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = this.route.data.subscribe(data => {
      this.editing = data['view'] === 'edit';
      this.error = '';
      this.loadCompany();
      if (this.editing) this.loadCurrencies();
    });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
    this.revokeLogoPreview();
  }

  get displayLogoUrl(): string | null {
    return this.logoPreview ?? this.auth.companyLogoUrl() ?? this.company?.logoUrl ?? null;
  }

  onLogoSelected(event: Event): void {
    this.logoError = '';
    const input = event.currentTarget as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (!file) return;
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) {
      this.logoError = 'Choose a JPEG, PNG, or WebP image no larger than 5 MB.';
      input.value = '';
      return;
    }

    input.value = '';
    this.revokeLogoPreview();
    this.selectedLogo = file;
    this.logoObjectUrl = URL.createObjectURL(file);
    this.logoPreview = this.logoObjectUrl;
  }

  uploadLogo(): void {
    const companyId = this.auth.companyId();
    if (!companyId || !this.selectedLogo) return;
    this.logoUploading = true;
    this.logoError = '';
    this.companies.uploadLogo(companyId, this.selectedLogo).subscribe({
      next: company => {
        this.company = company;
        this.auth.updateCompanyLogoUrl(company.logoUrl);
        this.selectedLogo = null;
        this.logoUploading = false;
        this.revokeLogoPreview();
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.logoError = errorMessage(error, 'Could not upload the company logo.');
        this.logoUploading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  setFormValue(key: string, value: unknown): void {
    this.form[key] = value;
  }

  submit(): void {
    const companyId = this.auth.companyId();
    const legalName = String(this.form['legalName'] ?? '').trim();
    const functionalCurrencyCode = String(this.form['functionalCurrencyCode'] ?? '').trim().toUpperCase();
    if (!companyId || !legalName || legalName.length > 250 ||
      !/^[A-Z]{3}$/.test(functionalCurrencyCode)) {
      this.error = 'Enter a legal name and choose a supported functional currency.';
      return;
    }

    const payload: UpdateCompanyPayload = {
      legalName,
      functionalCurrencyCode,
      phone: String(this.form['phone'] ?? '').trim(),
      email: String(this.form['email'] ?? '').trim(),
      website: String(this.form['website'] ?? '').trim(),
      address: String(this.form['address'] ?? '').trim(),
      cityRegion: String(this.form['cityRegion'] ?? '').trim(),
      postalCode: String(this.form['postalCode'] ?? '').trim(),
      country: String(this.form['country'] ?? '').trim(),
      taxId: String(this.form['taxId'] ?? '').trim(),
    };
    this.saving = true;
    this.error = '';
    this.companies.update(companyId, payload).subscribe({
      next: company => {
        this.company = company;
        this.form = this.toForm(company);
        this.saving = false;
        void this.router.navigateByUrl('/settings/company');
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save company settings.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    void this.router.navigateByUrl('/settings/company');
  }

  private loadCompany(): void {
    const companyId = this.auth.companyId();
    if (!companyId) {
      this.company = null;
      this.loading = false;
      this.error = 'Choose a company before viewing its settings.';
      return;
    }
    this.loading = true;
    this.companies.getById(companyId).subscribe({
      next: company => {
        this.company = company;
        this.form = {
          ...this.toForm(company),
        };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load company settings.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadCurrencies(): void {
    this.optionData.getOptions('currencies').subscribe({
      next: currencies => {
        this.options['currencies'] = currencies;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load currency options.');
        this.changeDetector.markForCheck();
      },
    });
  }

  private toForm(company: Company): Record<string, unknown> {
    return {
      legalName: company.legalName,
      functionalCurrencyCode: company.functionalCurrencyCode,
      phone: company.phone ?? '',
      email: company.email ?? '',
      website: company.website ?? '',
      address: company.address ?? '',
      cityRegion: company.cityRegion ?? '',
      postalCode: company.postalCode ?? '',
      country: company.country ?? '',
      taxId: company.taxId ?? '',
    };
  }

  private revokeLogoPreview(): void {
    if (this.logoObjectUrl) URL.revokeObjectURL(this.logoObjectUrl);
    this.logoObjectUrl = null;
    this.logoPreview = null;
  }
}
