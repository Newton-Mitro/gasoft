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

@Component({
  selector: 'app-company-settings-page',
  imports: [RouterLink, RecordFormComponent],
  templateUrl: './company-settings.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class CompanySettingsPageComponent implements OnInit, OnDestroy {
  readonly pageKey = 'company';
  readonly title = 'Company settings';
  readonly description = 'Update the company legal name and functional currency.';
  readonly fields: Field[] = [
    { key: 'legalName', label: 'Legal name', required: true },
    { key: 'functionalCurrencyCode', label: 'Functional currency', type: 'select', required: true, optionSource: 'currencies' },
  ];
  company: Company | null = null;
  form: Record<string, unknown> = {};
  options: Record<string, Choice[]> = {};
  editing = false;
  loading = false;
  saving = false;
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

    const payload: UpdateCompanyPayload = { legalName, functionalCurrencyCode };
    this.saving = true;
    this.error = '';
    this.companies.update(companyId, payload).subscribe({
      next: company => {
        this.company = company;
        this.form = { legalName: company.legalName, functionalCurrencyCode: company.functionalCurrencyCode };
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
          legalName: company.legalName,
          functionalCurrencyCode: company.functionalCurrencyCode,
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
}
