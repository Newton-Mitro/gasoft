import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, timeout } from 'rxjs';
import { SalesInvoice } from '../../sales-invoices/sales-invoice.model';
import { SalesInvoiceService } from '../../sales-invoices/sales-invoice.service';
import { Choice } from '../../../../shared/models/shared.models';
import { OptionDataService } from '../../../../shared/data/option-data.service';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { PageChange, PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import {
  CustomerRefundPayload,
  SalesCreditNote,
  SalesCreditNotePostingPayload,
} from '../sales-credit-note.model';
import { SalesCreditNotesService } from '../sales-credit-notes.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { FieldValidationDirective } from '../../../../shared/directives/field-validation.directive';
import { SearchableSelectDirective } from '../../../../shared/directives/searchable-select.directive';
import { InputComponent } from '../../../../shared/components/input/input.component';

interface CreditLineInput {
  invoiceLineId: number;
  description: string;
  originalQuantity: number;
  quantity: number;
}

@Component({
  selector: 'app-sales-credit-notes-page',
  imports: [FormsModule, PaginationComponent, FieldValidationDirective, SearchableSelectDirective, InputComponent],
  templateUrl: './sales-credit-notes.page.html',
  styleUrls: ['../../../../shared/styles/feature-page.css', './sales-credit-notes.page.css'],
})
export class SalesCreditNotesPageComponent implements OnInit {
  readonly notifications = inject(NotificationService);
  notes: SalesCreditNote[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  invoices: SalesInvoice[] = [];
  journals: Choice[] = [];
  periods: Choice[] = [];
  accounts: Choice[] = [];
  taxCodes: Choice[] = [];
  loading = true;
  supportDataLoading = true;
  supportDataError = '';
  saving = false;
  creating = false;
  error = '';
  success = '';
  selectedInvoiceId: number | null = null;
  creditNoteNumber = '';
  creditNoteDate = new Date().toISOString().slice(0, 10);
  reason = '';
  lines: CreditLineInput[] = [];
  postingNote: SalesCreditNote | null = null;
  refundingNote: SalesCreditNote | null = null;
  journalId: number | null = null;
  accountingPeriodId: number | null = null;
  receivableAccountId: number | null = null;
  taxAccountIds: Record<number, number | null> = {};
  refundNumber = '';
  refundDate = new Date().toISOString().slice(0, 10);
  refundAmount: number | null = null;
  refundJournalId: number | null = null;
  refundPeriodId: number | null = null;
  cashAccountId: number | null = null;

  constructor(
    private readonly service: SalesCreditNotesService,
    private readonly invoicesService: SalesInvoiceService,
    private readonly options: OptionDataService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadNotes();

    forkJoin({
      invoices: this.invoicesService.getAll(),
      journals: this.options.getOptions('journals'),
      periods: this.options.getOptions('periods'),
      accounts: this.options.getOptions('accounts'),
      taxCodes: this.options.getOptions('taxCodes'),
    }).pipe(timeout({ first: 15000 })).subscribe({
      next: result => {
        this.invoices = result.invoices;
        this.journals = result.journals;
        this.periods = result.periods;
        this.accounts = result.accounts;
        this.taxCodes = result.taxCodes;
        this.supportDataLoading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.supportDataError = '';
        this.notifications.error(errorMessage(error, 'Could not load credit note form options.'));
        this.supportDataLoading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  get creditableInvoices(): SalesInvoice[] {
    return this.invoices.filter(invoice =>
      invoice.status === 'Posted' && Number(invoice.creditedTotal) < Number(invoice.grandTotal));
  }

  get taxCodeIds(): number[] {
    return Object.keys(this.taxAccountIds).map(Number);
  }

  selectedInvoice(): SalesInvoice | undefined {
    return this.invoices.find(invoice => invoice.id === this.selectedInvoiceId);
  }

  canRefund(note: SalesCreditNote): boolean {
    return note.status === 'Posted' && Number(note.refundableAmount) > 0;
  }

  sourceUnitPrice(invoiceLineId: number): number | string {
    return this.selectedInvoice()?.lines?.find(line => line.id === invoiceLineId)?.unitPrice ?? 0;
  }

  beginCreate(): void {
    if (this.supportDataLoading || this.supportDataError) return;
    this.creating = true;
    this.postingNote = null;
    this.selectedInvoiceId = null;
    this.creditNoteNumber = '';
    this.creditNoteDate = new Date().toISOString().slice(0, 10);
    this.reason = '';
    this.lines = [];
    this.error = '';
  }

  selectInvoice(invoiceId: number | null): void {
    this.selectedInvoiceId = invoiceId;
    const invoice = this.invoices.find(item => item.id === invoiceId);
    this.lines = (invoice?.lines ?? []).map(line => ({
      invoiceLineId: line.id,
      description: line.description,
      originalQuantity: Number(line.quantity),
      quantity: 0,
    }));
  }

  create(): void {
    const selected = this.lines.filter(line => Number(line.quantity) > 0);
    if (!this.selectedInvoiceId || !this.creditNoteNumber.trim() || !this.creditNoteDate ||
      !this.reason.trim() || !selected.length ||
      selected.some(line => !Number.isFinite(Number(line.quantity)) ||
        Number(line.quantity) > line.originalQuantity)) {
      this.error = 'Choose a posted invoice, enter the credit note details, and set a valid quantity on at least one line.';
      return;
    }
    this.saving = true;
    this.error = '';
    this.service.create({
      salesInvoiceId: this.selectedInvoiceId,
      creditNoteNumber: this.creditNoteNumber.trim(),
      creditNoteDate: this.creditNoteDate,
      reason: this.reason.trim(),
      lines: selected.map(line => ({
        salesInvoiceLineId: line.invoiceLineId,
        quantity: Number(line.quantity),
      })),
    }).subscribe({
      next: () => {
        this.creating = false;
        this.saving = false;
        this.notifications.success('Sales credit note created as a draft.');
        this.refresh();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not create this credit note.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  beginPost(note: SalesCreditNote): void {
    if (note.status !== 'Draft' || this.supportDataLoading || this.supportDataError) return;
    this.postingNote = note;
    this.creating = false;
    this.journalId = null;
    this.accountingPeriodId = null;
    this.receivableAccountId = null;
    this.taxAccountIds = {};
    for (const id of new Set(note.lines
      .map(line => Number(line.taxCodeId))
      .filter(id => Number.isSafeInteger(id) && id > 0))) {
      this.taxAccountIds[id] = null;
    }
    this.error = '';
  }

  post(): void {
    const note = this.postingNote;
    if (!note) return;
    const payload: SalesCreditNotePostingPayload = {
      journalId: Number(this.journalId),
      accountingPeriodId: Number(this.accountingPeriodId),
      receivableAccountId: Number(this.receivableAccountId),
      taxAccounts: Object.entries(this.taxAccountIds).map(([taxCodeId, accountId]) => ({
        taxCodeId: Number(taxCodeId),
        accountId: Number(accountId),
      })),
    };
    if (![payload.journalId, payload.accountingPeriodId, payload.receivableAccountId,
      ...payload.taxAccounts.map(mapping => mapping.accountId)].every(isPositiveId)) {
      this.error = 'Choose a journal, accounting period, receivable account, and all required tax accounts.';
      return;
    }
    this.saving = true;
    this.error = '';
    this.service.post(note.id, payload).subscribe({
      next: () => {
        this.postingNote = null;
        this.saving = false;
        this.notifications.success('Sales credit note posted successfully.');
        this.refresh();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not post this credit note.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  beginRefund(note: SalesCreditNote): void {
    if (note.status !== 'Posted' || Number(note.refundableAmount) <= 0 ||
      this.supportDataLoading || this.supportDataError) return;
    this.refundingNote = note;
    this.creating = false;
    this.postingNote = null;
    this.refundNumber = '';
    this.refundDate = new Date().toISOString().slice(0, 10);
    this.refundAmount = Number(note.refundableAmount);
    this.refundJournalId = null;
    this.refundPeriodId = null;
    this.cashAccountId = null;
    this.error = '';
  }

  refund(): void {
    const note = this.refundingNote;
    if (!note) return;
    const payload: CustomerRefundPayload = {
      refundNumber: this.refundNumber.trim(),
      refundDate: this.refundDate,
      amount: Number(this.refundAmount),
      journalId: Number(this.refundJournalId),
      accountingPeriodId: Number(this.refundPeriodId),
      cashAccountId: Number(this.cashAccountId),
    };
    if (!payload.refundNumber || !payload.refundDate || !Number.isFinite(payload.amount) ||
      payload.amount <= 0 || payload.amount > Number(note.refundableAmount) ||
      ![payload.journalId, payload.accountingPeriodId, payload.cashAccountId].every(isPositiveId)) {
      this.error = 'Enter a refund number, date, valid amount within the remaining balance, journal, period, and cash account.';
      return;
    }
    this.saving = true;
    this.error = '';
    this.service.refund(note.id, payload).subscribe({
      next: () => {
        this.refundingNote = null;
        this.saving = false;
        this.notifications.success(`Refund ${payload.refundNumber} posted successfully.`);
        this.refresh();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not post this customer refund.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  cancel(): void {
    this.creating = false;
    this.postingNote = null;
    this.refundingNote = null;
    this.error = '';
  }

  private refresh(): void {
    forkJoin({ notes: this.service.getPage(this.page, this.pageSize), invoices: this.invoicesService.getAll() }).subscribe({
      next: result => {
        this.notes = result.notes.items;
        this.page = result.notes.page;
        this.pageSize = result.notes.pageSize;
        this.totalCount = result.notes.totalCount;
        this.totalPages = result.notes.totalPages;
        this.invoices = result.invoices;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'The operation succeeded, but credit notes could not be refreshed.'));
        this.changeDetector.markForCheck();
      },
    });
  }

  onPageChange(change: PageChange): void {
    this.page = change.page;
    this.pageSize = change.pageSize;
    this.loadNotes();
  }

  private loadNotes(): void {
    this.loading = true;
    this.service.getPage(this.page, this.pageSize).pipe(timeout({ first: 15000 })).subscribe({
      next: result => {
        this.notes = result.items;
        this.page = result.page;
        this.pageSize = result.pageSize;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load sales credit notes.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}

function isPositiveId(value: number): boolean {
  return Number.isSafeInteger(value) && value > 0;
}
