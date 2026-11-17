import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, timeout } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { errorMessage } from '../../../shared/utils/page-utils';
import { PageChange, PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { Choice, DocumentLineForm } from '../../../shared/models/shared.models';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { SalesQuote, SalesQuotePayload } from './sales-quote.model';
import { SalesQuotesService } from './sales-quotes.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';

interface CustomerOption {
  id: number;
  partyNo: string;
  displayName: string;
  isCustomer: boolean;
  email?: string | null;
}

@Component({
  selector: 'app-sales-quotes-page',
  imports: [FormsModule, PaginationComponent, FieldValidationDirective],
  templateUrl: './sales-quotes.page.html',
  styleUrls: ['../../../shared/styles/feature-page.css', './sales-quotes.page.css'],
})
export class SalesQuotesPageComponent implements OnInit {
  readonly notifications = inject(NotificationService);
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
    this.lines = [...this.lines, emptyLine()];
  }

  removeLine(index: number): void {
    if (this.lines.length > 1) this.lines = this.lines.filter((_, lineIndex) => lineIndex !== index);
  }

  save(): void {
    if (!this.customerId || !this.quoteNumber.trim() || !this.quoteDate || !this.validUntil ||
      !this.currencyCode || !Number.isFinite(Number(this.exchangeRate)) || Number(this.exchangeRate) <= 0 ||
      !this.lines.length || this.lines.some(line =>
        !line.accountId || !line.description.trim() || !Number.isFinite(Number(line.quantity)) ||
        Number(line.quantity) <= 0 || !Number.isFinite(Number(line.unitPrice)) || Number(line.unitPrice) < 0)) {
      this.error = 'Complete the quote details and add valid lines.';
      return;
    }
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
        this.editing = false;
        this.saving = false;
        this.notifications.success(`Quote ${this.quoteNumber} ${this.quoteId === null ? 'created' : 'updated'} successfully.`);
        this.refresh();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not save this sales quote.'));
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
    this.convertingQuote = quote;
    this.invoiceNumber = `INV-${quote.quoteNumber}`;
    this.invoiceDate = new Date().toISOString().slice(0, 10);
    this.dueDate = new Date(Date.now() + 30 * 86400000).toISOString().slice(0, 10);
    this.error = '';
  }

  convert(): void {
    if (!this.convertingQuote) return;
    const quote = this.convertingQuote;
    this.saving = true;
    this.error = '';
    this.service.convert(quote.id, this.invoiceNumber.trim(), this.invoiceDate, this.dueDate).subscribe({
      next: () => {
        this.convertingQuote = null;
        this.saving = false;
        this.notifications.success(`Quote ${quote.quoteNumber} converted to invoice ${this.invoiceNumber}.`);
        this.success = '';
        this.refresh();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not convert this quote to an invoice.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  cancelForm(): void {
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
