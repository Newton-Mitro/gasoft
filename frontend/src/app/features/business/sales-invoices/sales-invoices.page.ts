import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { BusinessService } from '../business.service';
import { SalesInvoicePostingPayload, TaxAccountMapping } from '../business.models';
import { errorMessage } from '../../../shared/utils/page-utils';
import { DocumentLineForm, Choice, Field, RecordFormMode } from '../../../shared/models/shared.models';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { SalesInvoice, SalesInvoicePayload } from './sales-invoice.model';
import { SalesInvoiceService } from './sales-invoice.service';

const DOCUMENT_OPTIONS = ['parties', 'currencies', 'accounts', 'taxCodes'];
const POSTING_OPTIONS = ['journals', 'periods', 'accounts'];

@Component({
  selector: 'app-sales-invoices-page',
  imports: [RouterLink, RecordFormComponent],
  templateUrl: './sales-invoices.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class SalesInvoicesPageComponent implements OnInit, OnDestroy {
  readonly pageKey = 'sales-invoices';
  readonly title = 'Sales invoices';
  readonly description = 'Create customer invoices and track their accounting status.';
  readonly fields: Field[] = [
    { key: 'partyId', label: 'Customer', type: 'select', required: true, optionSource: 'parties' },
    { key: 'invoiceNumber', label: 'Invoice number', required: true },
    { key: 'invoiceDate', label: 'Invoice date', type: 'date', required: true },
    { key: 'dueDate', label: 'Due date', type: 'date', required: true },
    { key: 'currencyCode', label: 'Currency code', type: 'select', required: true, optionSource: 'currencies' },
    { key: 'exchangeRate', label: 'Exchange rate', type: 'number', required: true },
  ];
  readonly indexPath = '/sales-invoices';
  rows: SalesInvoice[] = [];
  form: Record<string, unknown> = {};
  options: Record<string, Choice[]> = {};
  documentLines: DocumentLineForm[] = [];
  postingFields: Field[] = [];
  postingTaxCodeIds: number[] = [];
  postingInvoiceId: number | null = null;
  postingLoading = false;
  loading = false;
  saving = false;
  error = '';
  view: 'index' | 'create' | 'edit' = 'index';
  private routeSubscription?: Subscription;
  private currentRecordId: number | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly invoices: SalesInvoiceService,
    private readonly business: BusinessService,
    private readonly optionData: OptionDataService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = combineLatest([this.route.data, this.route.paramMap]).subscribe(([data, params]) => {
      const view = data['view'];
      this.view = view === 'create' || view === 'edit' ? view : 'index';
      this.currentRecordId = toRecordId(params);
      this.error = '';
      this.postingInvoiceId = null;
      if (this.view === 'create') {
        this.form = { exchangeRate: 1 };
        this.documentLines = [newDocumentLine()];
        this.loading = false;
        this.loadOptions(DOCUMENT_OPTIONS);
      } else if (this.view === 'edit') {
        this.loadOptions(DOCUMENT_OPTIONS);
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

  setDocumentLines(lines: DocumentLineForm[]): void {
    this.documentLines = lines;
  }

  submit(): void {
    const partyId = Number(this.form['partyId']);
    const exchangeRate = Number(this.form['exchangeRate']);
    if (!isPositiveId(partyId) || !String(this.form['invoiceNumber'] ?? '').trim() ||
      !String(this.form['invoiceDate'] ?? '') || !String(this.form['dueDate'] ?? '') ||
      !String(this.form['currencyCode'] ?? '').trim() || !Number.isFinite(exchangeRate) || exchangeRate <= 0 ||
      !this.documentLines.length || this.documentLines.some(line =>
        !isPositiveId(Number(line.accountId)) ||
        !String(line.description ?? '').trim() ||
        !Number.isFinite(Number(line.quantity)) || Number(line.quantity) <= 0 ||
        !Number.isFinite(Number(line.unitPrice)) || Number(line.unitPrice) < 0 ||
        (line.taxCodeId !== null && line.taxCodeId !== undefined && !isPositiveId(Number(line.taxCodeId))))) {
      this.error = 'Complete the invoice details and add valid invoice lines.';
      return;
    }

    const payload: SalesInvoicePayload = {
      partyId,
      invoiceNumber: String(this.form['invoiceNumber']).trim(),
      invoiceDate: String(this.form['invoiceDate']),
      dueDate: String(this.form['dueDate']),
      currencyCode: String(this.form['currencyCode']),
      exchangeRate,
      lines: this.documentLines.map(line => ({
        accountId: Number(line.accountId),
        taxCodeId: line.taxCodeId ? Number(line.taxCodeId) : null,
        description: line.description.trim(),
        quantity: Number(line.quantity),
        unitPrice: Number(line.unitPrice),
      })),
    };

    let request;
    if (this.view === 'edit') {
      if (this.currentRecordId === null) {
        this.error = 'A valid sales invoice ID is required to update this record.';
        return;
      }
      request = this.invoices.update(this.currentRecordId, payload);
    } else {
      request = this.invoices.create(payload);
    }
    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save this sales invoice.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  openPostForm(invoice: SalesInvoice): void {
    if (invoice.status !== 'Draft') return;
    this.postingInvoiceId = invoice.id;
    this.postingLoading = true;
    this.error = '';
    this.invoices.getById(invoice.id).subscribe({
      next: detail => {
        this.postingTaxCodeIds = [...new Set((detail.lines ?? [])
          .map(line => Number(line.taxCodeId))
          .filter(id => isPositiveId(id)))];
        this.postingFields = [
          { key: 'journalId', label: 'Journal', type: 'select', required: true, optionSource: 'journals' },
          { key: 'accountingPeriodId', label: 'Accounting period', type: 'select', required: true, optionSource: 'periods' },
          { key: 'receivableAccountId', label: 'Receivable account', type: 'select', required: true, optionSource: 'accounts' },
          ...this.postingTaxCodeIds.map(id => ({
            key: `taxAccount_${id}`,
            label: `Tax account for code ${id}`,
            type: 'select' as const,
            required: true,
            optionSource: 'accounts' as const,
          })),
        ];
        this.form = {};
        this.postingLoading = false;
        this.loadOptions(POSTING_OPTIONS);
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load sales invoice posting details.');
        this.postingInvoiceId = null;
        this.postingLoading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  submitPost(): void {
    if (this.postingInvoiceId === null) return;
    const payload: SalesInvoicePostingPayload = {
      journalId: Number(this.form['journalId']),
      accountingPeriodId: Number(this.form['accountingPeriodId']),
      receivableAccountId: Number(this.form['receivableAccountId']),
      taxAccounts: this.postingTaxCodeIds.map(taxCodeId => ({
        taxCodeId,
        accountId: Number(this.form[`taxAccount_${taxCodeId}`]),
      } satisfies TaxAccountMapping)),
    };
    if (![payload.journalId, payload.accountingPeriodId, payload.receivableAccountId,
      ...payload.taxAccounts.map(mapping => mapping.accountId)].every(isPositiveId)) {
      this.error = 'Choose a journal, accounting period, receivable account, and all required tax accounts.';
      return;
    }
    const invoiceId = this.postingInvoiceId;
    this.saving = true;
    this.error = '';
    this.business.postSalesInvoice(invoiceId, payload).subscribe({
      next: () => {
        this.postingInvoiceId = null;
        this.saving = false;
        this.loadRows();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not post this sales invoice.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadOptions(sources: string[]): void {
    this.options = {};
    for (const source of sources) {
      this.optionData.getOptions(source).subscribe({
        next: options => {
          this.options = { ...this.options, [source]: options };
          this.changeDetector.markForCheck();
        },
        error: error => {
          this.error = errorMessage(error, `Could not load ${source} options.`);
          this.changeDetector.markForCheck();
        },
      });
    }
  }

  private loadRows(): void {
    this.loading = true;
    this.invoices.getAll().subscribe({
      next: rows => {
        this.rows = rows;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load sales invoices.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.error = 'A valid sales invoice ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.invoices.getById(this.currentRecordId).subscribe({
      next: invoice => {
        if (invoice.status !== 'Draft') {
          this.error = 'Only draft sales invoices can be edited.';
          this.loading = false;
          this.changeDetector.markForCheck();
          return;
        }
        if (!invoice.lines?.length) {
          this.error = 'This draft has no editable invoice lines.';
          this.loading = false;
          this.changeDetector.markForCheck();
          return;
        }
        this.form = {
          partyId: invoice.partyId,
          invoiceNumber: invoice.invoiceNumber,
          invoiceDate: invoice.invoiceDate,
          dueDate: invoice.dueDate,
          currencyCode: invoice.currencyCode,
          exchangeRate: invoice.exchangeRate,
        };
        this.documentLines = invoice.lines.map(line => ({
          accountId: line.accountId,
          taxCodeId: line.taxCodeId,
          description: line.description,
          quantity: Number(line.quantity),
          unitPrice: Number(line.unitPrice),
        }));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load this sales invoice.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}

function newDocumentLine(): DocumentLineForm {
  return { accountId: null, taxCodeId: null, description: '', quantity: 1, unitPrice: 0 };
}

function toRecordId(params: ParamMap): number | null {
  const id = Number(params.get('recordId'));
  return isPositiveId(id) ? id : null;
}

function isPositiveId(value: number): boolean {
  return Number.isSafeInteger(value) && value > 0;
}
