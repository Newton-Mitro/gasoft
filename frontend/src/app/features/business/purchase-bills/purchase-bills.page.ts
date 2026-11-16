import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { BusinessService } from '../business.service';
import { PurchaseBillPostingPayload, TaxAccountMapping } from '../business.models';
import { errorMessage } from '../../../shared/utils/page-utils';
import { Choice, DocumentLineForm, Field, RecordFormMode } from '../../../shared/models/shared.models';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { PageChange, PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { PurchaseBill, PurchaseBillPayload } from './purchase-bill.model';
import { PurchaseBillService } from './purchase-bill.service';

const DOCUMENT_OPTIONS = ['parties', 'currencies', 'accounts', 'taxCodes'];
const POSTING_OPTIONS = ['journals', 'periods', 'accounts'];

@Component({
  selector: 'app-purchase-bills-page',
  imports: [RouterLink, RecordFormComponent, PaginationComponent],
  templateUrl: './purchase-bills.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class PurchaseBillsPageComponent implements OnInit, OnDestroy {
  readonly pageKey = 'purchase-bills';
  readonly title = 'Purchase bills';
  readonly description = 'Record supplier bills and the expense or asset account for each item.';
  readonly fields: Field[] = [
    { key: 'partyId', label: 'Supplier', type: 'select', required: true, optionSource: 'parties' },
    { key: 'billNumber', label: 'Bill number', required: true },
    { key: 'billDate', label: 'Bill date', type: 'date', required: true },
    { key: 'dueDate', label: 'Due date', type: 'date', required: true },
    { key: 'currencyCode', label: 'Currency code', type: 'select', required: true, optionSource: 'currencies' },
    { key: 'exchangeRate', label: 'Exchange rate', type: 'number', required: true },
  ];
  readonly indexPath = '/purchase-bills';
  rows: PurchaseBill[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  form: Record<string, unknown> = {};
  options: Record<string, Choice[]> = {};
  documentLines: DocumentLineForm[] = [];
  postingFields: Field[] = [];
  postingTaxCodeIds: number[] = [];
  postingBillId: number | null = null;
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
    private readonly bills: PurchaseBillService,
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
      this.postingBillId = null;
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
    if (!isPositiveId(partyId) || !String(this.form['billNumber'] ?? '').trim() ||
      !String(this.form['billDate'] ?? '') || !String(this.form['dueDate'] ?? '') ||
      !String(this.form['currencyCode'] ?? '').trim() || !Number.isFinite(exchangeRate) || exchangeRate <= 0 ||
      !this.documentLines.length || this.documentLines.some(line =>
        !isPositiveId(Number(line.accountId)) ||
        !String(line.description ?? '').trim() ||
        !Number.isFinite(Number(line.quantity)) || Number(line.quantity) <= 0 ||
        !Number.isFinite(Number(line.unitPrice)) || Number(line.unitPrice) < 0 ||
        (line.taxCodeId !== null && line.taxCodeId !== undefined && !isPositiveId(Number(line.taxCodeId))))) {
      this.error = 'Complete the bill details and add valid bill lines.';
      return;
    }

    const payload: PurchaseBillPayload = {
      partyId,
      billNumber: String(this.form['billNumber']).trim(),
      billDate: String(this.form['billDate']),
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
        this.error = 'A valid purchase bill ID is required to update this record.';
        return;
      }
      request = this.bills.update(this.currentRecordId, payload);
    } else {
      request = this.bills.create(payload);
    }
    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save this purchase bill.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    if (this.view !== 'index') {
      void this.router.navigateByUrl(this.indexPath);
      return;
    }
    this.postingBillId = null;
    this.error = '';
  }

  openPostForm(bill: PurchaseBill): void {
    if (bill.status !== 'Draft') return;
    this.postingBillId = bill.id;
    this.postingLoading = true;
    this.error = '';
    this.bills.getById(bill.id).subscribe({
      next: detail => {
        this.postingTaxCodeIds = [...new Set((detail.lines ?? [])
          .map(line => Number(line.taxCodeId))
          .filter(isPositiveId))];
        this.postingFields = [
          { key: 'journalId', label: 'Journal', type: 'select', required: true, optionSource: 'journals' },
          { key: 'accountingPeriodId', label: 'Accounting period', type: 'select', required: true, optionSource: 'periods' },
          { key: 'payableAccountId', label: 'Payable account', type: 'select', required: true, optionSource: 'accounts' },
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
        this.error = errorMessage(error, 'Could not load purchase bill posting details.');
        this.postingBillId = null;
        this.postingLoading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  submitPost(): void {
    if (this.postingBillId === null) return;
    const payload: PurchaseBillPostingPayload = {
      journalId: Number(this.form['journalId']),
      accountingPeriodId: Number(this.form['accountingPeriodId']),
      payableAccountId: Number(this.form['payableAccountId']),
      taxAccounts: this.postingTaxCodeIds.map(taxCodeId => ({
        taxCodeId,
        accountId: Number(this.form[`taxAccount_${taxCodeId}`]),
      } satisfies TaxAccountMapping)),
    };
    if (![payload.journalId, payload.accountingPeriodId, payload.payableAccountId,
      ...payload.taxAccounts.map(mapping => mapping.accountId)].every(isPositiveId)) {
      this.error = 'Choose a journal, accounting period, payable account, and all required tax accounts.';
      return;
    }
    const billId = this.postingBillId;
    this.saving = true;
    this.error = '';
    this.business.postPurchaseBill(billId, payload).subscribe({
      next: () => {
        this.postingBillId = null;
        this.saving = false;
        this.loadRows();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not post this purchase bill.');
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
    this.bills.getPage(this.page, this.pageSize).subscribe({
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
        this.error = errorMessage(error, 'Could not load purchase bills.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  onPageChange(change: PageChange): void {
    this.page = change.page;
    this.pageSize = change.pageSize;
    this.loadRows();
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.error = 'A valid purchase bill ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.bills.getById(this.currentRecordId).subscribe({
      next: bill => {
        if (bill.status !== 'Draft') {
          this.error = 'Only draft purchase bills can be edited.';
          this.loading = false;
          this.changeDetector.markForCheck();
          return;
        }
        if (!bill.lines?.length) {
          this.error = 'This draft has no editable bill lines.';
          this.loading = false;
          this.changeDetector.markForCheck();
          return;
        }
        this.form = {
          partyId: bill.partyId,
          billNumber: bill.billNumber,
          billDate: bill.billDate,
          dueDate: bill.dueDate,
          currencyCode: bill.currencyCode,
          exchangeRate: bill.exchangeRate,
        };
        this.documentLines = bill.lines.map(line => ({
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
        this.error = errorMessage(error, 'Could not load this purchase bill.');
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
