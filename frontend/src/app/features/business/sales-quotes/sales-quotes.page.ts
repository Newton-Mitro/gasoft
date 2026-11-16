import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { errorMessage } from '../../../shared/utils/page-utils';
import { Choice, DocumentLineForm } from '../../../shared/models/shared.models';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { SalesQuote, SalesQuotePayload } from './sales-quote.model';
import { SalesQuotesService } from './sales-quotes.service';

interface CustomerOption {
  id: number;
  partyNo: string;
  displayName: string;
  isCustomer: boolean;
  email?: string | null;
}

@Component({
  selector: 'app-sales-quotes-page',
  imports: [FormsModule],
  templateUrl: './sales-quotes.page.html',
  styleUrl: './sales-quotes.page.css',
})
export class SalesQuotesPageComponent implements OnInit {
  quotes: SalesQuote[] = [];
  customers: CustomerOption[] = [];
  accounts: Choice[] = [];
  taxCodes: Choice[] = [];
  currencies: Choice[] = [];
  loading = true;
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
    forkJoin({
      parties: this.api.get<unknown>('/api/v1/parties/', false),
      accounts: this.options.getOptions('accounts'),
      taxCodes: this.options.getOptions('taxCodes'),
      currencies: this.options.getOptions('currencies'),
      quotes: this.service.getAll(),
    }).subscribe({
      next: result => {
        const rows = Array.isArray(result.parties) ? result.parties : [];
        this.customers = rows.filter(isCustomerOption);
        this.accounts = result.accounts;
        this.taxCodes = result.taxCodes;
        this.currencies = result.currencies;
        this.quotes = result.quotes;
        this.loading = false;
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load sales quotes.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  beginCreate(): void {
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
    if (quote.status !== 'Draft') return;
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
        this.refresh();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save this sales quote.');
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
        this.success = `Quote ${quote.quoteNumber} ${statusText}.`;
        this.refresh();
      },
      error: error => {
        this.error = errorMessage(error, `Could not ${action} this quote.`);
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
        this.success = `Quote ${quote.quoteNumber} emailed to ${result.sentTo}.`;
        this.refresh();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not email this quote.');
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
        this.success = `Quote ${quote.quoteNumber} converted to invoice ${this.invoiceNumber}.`;
        this.refresh();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not convert this quote to an invoice.');
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
    this.service.getAll().subscribe({
      next: quotes => {
        this.quotes = quotes;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'The action completed, but quotes could not be refreshed.');
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
