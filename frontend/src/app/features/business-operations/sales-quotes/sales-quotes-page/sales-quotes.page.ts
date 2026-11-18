import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { forkJoin, timeout } from 'rxjs';
import { ApiService } from '../../../../core/api.service';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { PageChange, PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { Choice, DocumentLineForm } from '../../../../shared/models/shared.models';
import { OptionDataService } from '../../../../shared/data/option-data.service';
import { SalesQuote, SalesQuotePayload } from '../sales-quote.model';
import { SalesQuotesService } from '../sales-quotes.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { FieldValidationDirective } from '../../../../shared/directives/field-validation.directive';
import { SearchableSelectDirective } from '../../../../shared/directives/searchable-select.directive';
import { InputComponent } from '../../../../shared/components/input/input.component';
import { FormValidationErrors, FormValidationService } from '../../../../shared/services/form-validation.service';

interface CustomerOption {
  id: number;
  partyNo: string;
  displayName: string;
  isCustomer: boolean;
  email?: string | null;
}

@Component({
  selector: 'app-sales-quotes-page',
  imports: [FormsModule, PaginationComponent, FieldValidationDirective, SearchableSelectDirective, InputComponent],
  templateUrl: './sales-quotes.page.html',
  styleUrls: ['../../../../shared/styles/feature-page.css', './sales-quotes.page.css'],
})
export class SalesQuotesPageComponent implements OnInit {
  readonly notifications = inject(NotificationService);
  private readonly formValidation = inject(FormValidationService);
  quotes: SalesQuote[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  customers: CustomerOption[] = [];
  accounts: Choice[] = [];
  taxCodes: Choice[] = [];
  currencies: Choice[] = [];
  loading = true;
  supportDataLoading = true;
  supportDataError = '';
  editing = false;
  saving = false;
  error = '';
  success = '';
  quoteId: number | null = null;
  customerId: number | null = null;
  quoteNumber = '';
  quoteDate = new Date().toISOString().slice(0, 10);
  validUntil = new Date(Date.now() + 30 * 86400000).toISOString().slice(0, 10);
  currencyCode = '';
  exchangeRate = 1;
  lines: DocumentLineForm[] = [];
  convertingQuote: SalesQuote | null = null;
  invoiceNumber = '';
  invoiceDate = '';
  dueDate = '';
  sendingQuoteId: number | null = null;

  constructor(
    private readonly service: SalesQuotesService,
    private readonly options: OptionDataService,
    private readonly api: ApiService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadQuotes();

    forkJoin({
      parties: this.api.get<unknown>('/api/v1/parties/', false),
      accounts: this.options.getOptions('accounts'),
      taxCodes: this.options.getOptions('taxCodes'),
      currencies: this.options.getOptions('currencies'),
    }).pipe(timeout({ first: 15000 })).subscribe({
      next: result => {
        const rows = Array.isArray(result.parties) ? result.parties : [];
        this.customers = rows.filter(isCustomerOption);
        this.accounts = result.accounts;
        this.taxCodes = result.taxCodes;
        this.currencies = result.currencies;
        this.supportDataLoading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.supportDataError = '';
        this.notifications.error(errorMessage(error, 'Could not load sales quote form options.'));
        this.supportDataLoading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  beginCreate(): void {
    if (this.supportDataLoading || this.supportDataError) return;
    this.formValidation.clear();
    this.quoteId = null;
    this.customerId = null;
    this.quoteNumber = '';
    this.quoteDate = new Date().toISOString().slice(0, 10);
    this.validUntil = new Date(Date.now() + 30 * 86400000).toISOString().slice(0, 10);
    this.currencyCode = '';
    this.exchangeRate = 1;
    this.lines = [emptyLine()];
    this.editing = true;
    this.error = '';
    this.success = '';
  }

  beginEdit(quote: SalesQuote): void {
    if (quote.status !== 'Draft' || this.supportDataLoading || this.supportDataError) return;
    this.formValidation.clear();
    this.quoteId = quote.id;
    this.customerId = quote.customerId;
    this.quoteNumber = quote.quoteNumber;
    this.quoteDate = quote.quoteDate;
    this.validUntil = quote.validUntil;
    this.currencyCode = quote.currencyCode;
    this.exchangeRate = Number(quote.exchangeRate);
    this.lines = quote.lines.map(line => ({
      accountId: line.accountId,
      taxCodeId: line.taxCodeId,
      description: line.description,
      quantity: Number(line.quantity),
      unitPrice: Number(line.unitPrice),
    }));
    this.editing = true;
    this.error = '';
  }

  addLine(): void {
    this.formValidation.clear();
    this.lines = [...this.lines, emptyLine()];
  }

  removeLine(index: number): void {
    if (this.lines.length > 1) {
      this.formValidation.clear();
      this.lines = this.lines.filter((_, lineIndex) => lineIndex !== index);
    }
  }

  save(): void {
    const validationErrors = validateQuote(this);
    this.formValidation.setErrors(validationErrors);
    if (Object.keys(validationErrors).length || this.customerId === null) return;

    const payload: SalesQuotePayload = {
      customerId: this.customerId,
      quoteNumber: this.quoteNumber.trim(),
      quoteDate: this.quoteDate,
      validUntil: this.validUntil,
      currencyCode: this.currencyCode,
      exchangeRate: Number(this.exchangeRate),
      lines: this.lines.map(line => ({
        accountId: Number(line.accountId),
        taxCodeId: line.taxCodeId ? Number(line.taxCodeId) : null,
        description: line.description.trim(),
        quantity: Number(line.quantity),
        unitPrice: Number(line.unitPrice),
      })),
    };
    const request = this.quoteId === null
      ? this.service.create(payload)
      : this.service.update(this.quoteId, payload);
    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.formValidation.clear();
        this.editing = false;
        this.saving = false;
        this.notifications.success(`Quote ${this.quoteNumber} ${this.quoteId === null ? 'created' : 'updated'} successfully.`);
        this.refresh();
      },
      error: error => {
        const validationErrors = serverValidationErrors(error);
        if (Object.keys(validationErrors).length) {
          this.formValidation.setErrors(validationErrors);
        } else {
          this.formValidation.clear();
          this.notifications.error(
            errorMessage(error, 'Could not save this sales quote.') ||
              'Please review the quote details and try again.',
          );
        }
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  act(quote: SalesQuote, action: 'accept' | 'decline' | 'cancel'): void {
    this.error = '';
    this.success = '';
    this.service.transition(quote.id, action).subscribe({
      next: () => {
        const statusText = action === 'accept' ? 'accepted' : action === 'decline' ? 'declined' : 'cancelled';
        this.notifications.success(`Quote ${quote.quoteNumber} ${statusText}.`);
        this.success = '';
        this.refresh();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, `Could not ${action} this quote.`));
        this.changeDetector.markForCheck();
      },
    });
  }

  sendQuote(quote: SalesQuote): void {
    this.error = '';
    this.success = '';
    this.sendingQuoteId = quote.id;
    this.service.sendQuote(quote.id).subscribe({
      next: result => {
        this.sendingQuoteId = null;
        this.notifications.success(`Quote ${quote.quoteNumber} emailed to ${result.sentTo}.`);
        this.success = '';
        this.refresh();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not email this quote.'));
        this.sendingQuoteId = null;
        this.changeDetector.markForCheck();
      },
    });
  }

  getCustomerEmail(customerId: number): string | null {
    return this.customers.find(customer => customer.id === customerId)?.email ?? null;
  }

  beginConvert(quote: SalesQuote): void {
    this.formValidation.clear();
    this.convertingQuote = quote;
    this.invoiceNumber = `INV-${quote.quoteNumber}`;
    this.invoiceDate = new Date().toISOString().slice(0, 10);
    this.dueDate = new Date(Date.now() + 30 * 86400000).toISOString().slice(0, 10);
    this.error = '';
  }

  convert(): void {
    if (!this.convertingQuote) return;
    const quote = this.convertingQuote;
    const validationErrors = validateConversion(this);
    this.formValidation.setErrors(validationErrors);
    if (Object.keys(validationErrors).length) return;

    this.saving = true;
    this.error = '';
    this.service.convert(quote.id, this.invoiceNumber.trim(), this.invoiceDate, this.dueDate).subscribe({
      next: () => {
        this.formValidation.clear();
        this.convertingQuote = null;
        this.saving = false;
        this.notifications.success(`Quote ${quote.quoteNumber} converted to invoice ${this.invoiceNumber}.`);
        this.success = '';
        this.refresh();
      },
      error: error => {
        const validationErrors = serverValidationErrors(error);
        if (Object.keys(validationErrors).length) {
          this.formValidation.setErrors(validationErrors);
        } else {
          this.formValidation.clear();
          this.notifications.error(
            errorMessage(error, 'Could not convert this quote to an invoice.') ||
              'Please review the invoice details and try again.',
          );
        }
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  cancelForm(): void {
    this.formValidation.clear();
    this.editing = false;
    this.convertingQuote = null;
    this.error = '';
  }

  private refresh(): void {
    this.loadQuotes('The action completed, but quotes could not be refreshed.');
  }

  onPageChange(change: PageChange): void {
    this.page = change.page;
    this.pageSize = change.pageSize;
    this.loadQuotes();
  }

  private loadQuotes(failureMessage = 'Could not load sales quotes.'): void {
    this.loading = true;
    this.service.getPage(this.page, this.pageSize).pipe(timeout({ first: 15000 })).subscribe({
      next: result => {
        this.quotes = result.items;
        this.page = result.page;
        this.pageSize = result.pageSize;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, failureMessage));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}

function validateQuote(page: SalesQuotesPageComponent): FormValidationErrors {
  const errors: FormValidationErrors = {};
  const exchangeRate = Number(page.exchangeRate);
  if (!Number.isSafeInteger(page.customerId) || !page.customerId || page.customerId < 1) {
    errors['customer'] = ['Select a customer.'];
  }
  if (!page.quoteNumber.trim()) errors['quoteNumber'] = ['Enter a quote number.'];
  else if (page.quoteNumber.trim().length > 40) {
    errors['quoteNumber'] = ['Quote number must be 40 characters or fewer.'];
  }
  if (!isDateOnly(page.quoteDate)) errors['quoteDate'] = ['Enter a valid quote date.'];
  if (!isDateOnly(page.validUntil)) {
    errors['validUntil'] = ['Enter a valid expiration date.'];
  } else if (isDateOnly(page.quoteDate) && page.validUntil < page.quoteDate) {
    errors['validUntil'] = ['Expiration date must be on or after the quote date.'];
  }
  if (!page.currencyCode) errors['currency'] = ['Select a currency.'];
  if (!Number.isFinite(exchangeRate) || exchangeRate <= 0) {
    errors['exchangeRate'] = ['Exchange rate must be greater than zero.'];
  }
  if (!page.lines.length) errors['description-0'] = ['Add at least one quote line.'];
  page.lines.forEach((line, index) => {
    if (!Number.isSafeInteger(Number(line.accountId)) || Number(line.accountId) < 1) {
      errors[`account-${index}`] = ['Select a revenue account.'];
    }
    if (!line.description.trim()) errors[`description-${index}`] = ['Enter a line description.'];
    if (line.description.length > 500) {
      errors[`description-${index}`] = ['Description must be 500 characters or fewer.'];
    }
    if (!Number.isFinite(Number(line.quantity)) || Number(line.quantity) <= 0) {
      errors[`quantity-${index}`] = ['Quantity must be greater than zero.'];
    }
    if (!Number.isFinite(Number(line.unitPrice)) || Number(line.unitPrice) < 0) {
      errors[`price-${index}`] = ['Unit price cannot be negative.'];
    }
  });
  return errors;
}

function validateConversion(page: SalesQuotesPageComponent): FormValidationErrors {
  const errors: FormValidationErrors = {};
  if (!page.invoiceNumber.trim()) errors['invoiceNumber'] = ['Enter an invoice number.'];
  else if (page.invoiceNumber.trim().length > 40) {
    errors['invoiceNumber'] = ['Invoice number must be 40 characters or fewer.'];
  }
  if (!isDateOnly(page.invoiceDate)) errors['invoiceDate'] = ['Enter a valid invoice date.'];
  if (!isDateOnly(page.dueDate)) {
    errors['dueDate'] = ['Enter a valid due date.'];
  } else if (isDateOnly(page.invoiceDate) && page.dueDate < page.invoiceDate) {
    errors['dueDate'] = ['Due date must be on or after the invoice date.'];
  }
  return errors;
}

function serverValidationErrors(error: unknown): FormValidationErrors {
  if (!(error instanceof HttpErrorResponse) ||
    (error.status !== 400 && error.status !== 422) ||
    !error.error || typeof error.error !== 'object' ||
    !('errors' in error.error) || !error.error.errors || typeof error.error.errors !== 'object') {
    return {};
  }

  return Object.fromEntries(
    Object.entries(error.error.errors).flatMap(([field, messages]) => {
      const fieldMessages = Array.isArray(messages)
        ? messages.filter((message): message is string => typeof message === 'string' && !!message.trim())
        : typeof messages === 'string' && messages.trim()
          ? [messages]
          : [];
      return fieldMessages.length ? [[field, fieldMessages]] : [];
    }),
  );
}

function isDateOnly(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const date = new Date(`${value}T00:00:00.000Z`);
  return !Number.isNaN(date.valueOf()) && date.toISOString().slice(0, 10) === value;
}

function emptyLine(): DocumentLineForm {
  return { accountId: null, taxCodeId: null, description: '', quantity: 1, unitPrice: 0 };
}

function isCustomerOption(value: unknown): value is CustomerOption {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return Number.isSafeInteger(row['id']) && typeof row['partyNo'] === 'string' &&
    typeof row['displayName'] === 'string' && row['isCustomer'] === true &&
    (row['email'] === undefined || row['email'] === null || typeof row['email'] === 'string');
}
