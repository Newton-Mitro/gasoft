import { Component, OnInit, signal, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth.service';
import { errorMessage, registrationError } from '../../../shared/utils/page-utils';
import { NotificationService } from '../../../shared/services/notification.service';
import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { SearchableSelectDirective } from '../../../shared/directives/searchable-select.directive';

interface Currency {
  code: string;
  name: string;
}

@Component({
  selector: 'app-register-page',
  imports: [FormsModule, RouterLink, FieldValidationDirective, SearchableSelectDirective],
  template: `
    <main class="auth-page">
      <section class="auth-brand">
        <a class="brand" routerLink="/login"><img class="brand-logo" src="/logo.png" alt="gasoft logo" /> gasoft</a>
        <div class="brand-copy">
          <p class="eyebrow">A BETTER WAY TO BALANCE</p>
          <h1>Your business,<br />beautifully organized.</h1>
          <p>Set up a secure finance workspace and get started with a ready-to-use ledger.</p>
        </div>
        <span class="brand-foot">Your records stay yours</span>
      </section>
      <section class="auth-form-wrap register-wrap">
        <form class="auth-card register-card" (ngSubmit)="submit()">
          <p class="eyebrow">GET STARTED</p>
          <h2>Create your workspace</h2>
          <p class="muted">Your starter accounts and accounting periods will be created automatically.</p>
          @if (error()) { <div class="alert error" role="alert" aria-live="polite">{{ error() }}</div> }
          @if (showSignIn()) { <p class="auth-switch">Already have an account? <a routerLink="/login">Sign in to your workspace</a>.</p> }
          <div class="form-row">
            <label>Workspace name<input name="tenantName" [(ngModel)]="tenantName" (ngModelChange)="onTenantNameChange($event)" required /></label>
            <label>Workspace URL<input name="slug" [(ngModel)]="slug" (ngModelChange)="slugEdited = true" required /></label>
          </div>
          <div class="form-row">
            <label>Company code<input name="companyCode" [(ngModel)]="companyCode" required /></label>
            <label>Legal name<input name="legalName" [(ngModel)]="legalName" required /></label>
          </div>
          <label>Functional currency
            <select appSearchableSelect name="functionalCurrencyCode" [(ngModel)]="currencyCode" [disabled]="currencyLoading() || currencyLoadFailed()" required>
              @if (currencyLoading()) { <option value="" disabled>Loading currencies…</option> }
              @else if (currencyLoadFailed()) { <option value="" disabled>Could not load currencies</option> }
              @else { <option value="" disabled>Select a currency</option> }
              @for (currency of currencies(); track currency.code) {
                <option [value]="currency.code">{{ currency.code }} · {{ currency.name }}</option>
              }
            </select>
            @if (currencyLoadFailed()) {
              <button class="button secondary" type="button" (click)="loadCurrencies()">Retry loading currencies</button>
            }
          </label>
          <div class="form-row">
            <label>Your name<input name="ownerDisplayName" [(ngModel)]="ownerDisplayName" required /></label>
            <label>Email address<input name="email" type="email" [(ngModel)]="email" autocomplete="email" required /></label>
          </div>
          <label>Password<input name="password" type="password" [(ngModel)]="password" minlength="12" autocomplete="new-password" required /></label>
          <button class="button primary full" [disabled]="busy() || currencyLoading() || currencyLoadFailed() || !currencies().length">{{ busy() ? 'Creating workspace…' : 'Create workspace' }}</button>
          <p class="auth-switch">Already have an account? <a routerLink="/login">Sign in</a></p>
        </form>
      </section>
    </main>
  `,
})
export class RegisterPage implements OnInit {
  readonly notifications = inject(NotificationService);
  readonly currencies = signal<Currency[]>([]);
  readonly currencyLoading = signal(true);
  readonly currencyLoadFailed = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly showSignIn = signal(false);
  tenantName = '';
  slug = '';
  slugEdited = false;
  companyCode = '';
  legalName = '';
  currencyCode = '';
  ownerDisplayName = '';
  email = '';
  password = '';
  constructor(
    private readonly http: HttpClient,
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.loadCurrencies();
  }

  loadCurrencies(): void {
    this.currencyLoading.set(true);
    this.currencyLoadFailed.set(false);
    this.http.get<Currency[]>('/api/v1/currencies').subscribe({
      next: currencies => {
        this.currencies.set(currencies);
        this.currencyCode = currencies[0]?.code ?? '';
        this.currencyLoading.set(false);
      },
      error: error => {
        this.error.set('');
        this.notifications.error(errorMessage(error, 'Could not load supported currencies.'));
        this.currencyLoading.set(false);
        this.currencyLoadFailed.set(true);
      },
    });
  }

  onTenantNameChange(value: string): void {
    if (!this.slugEdited) {
      this.slug = value.toLowerCase().trim().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
    }
  }

  submit(): void {
    this.busy.set(true);
    this.error.set('');
    this.showSignIn.set(false);
    this.auth.register({
      tenantName: this.tenantName.trim(),
      slug: this.slug.trim(),
      companyCode: this.companyCode.trim(),
      legalName: this.legalName.trim(),
      functionalCurrencyCode: this.currencyCode,
      ownerDisplayName: this.ownerDisplayName.trim(),
      email: this.email.trim(),
      password: this.password,
    }).subscribe({
      next: () => {
        this.notifications.success('Your workspace was created successfully.');
        void this.router.navigate(['/dashboard']);
      },
      error: error => {
        const result = registrationError(error);
        this.error.set('');
        this.notifications.error(result.message);
        this.showSignIn.set(result.showSignIn);
        this.busy.set(false);
      },
    });
  }
}
