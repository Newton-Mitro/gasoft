import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { BusinessService } from '../../data-access/business.service';
import { PaymentAllocationPayload, PaymentPostingPayload } from '../../data-access/business.models';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { Choice, Field, RecordFormMode } from '../../../../shared/models/shared.models';
import { RecordFormComponent } from '../../../../shared/components/record-form/record-form.component';
import { PageChange, PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { OptionDataService } from '../../../../shared/data/option-data.service';
import { Payment, PaymentPayload } from '../payment.model';
import { PaymentService } from '../payment.service';
import { NotificationService } from '../../../../shared/services/notification.service';

const PAYMENT_OPTIONS = ['parties', 'currencies'];
const POSTING_OPTIONS = ['journals', 'periods', 'accounts'];

@Component({
  selector: 'app-payments-page',
  imports: [RouterLink, RecordFormComponent, PaginationComponent],
  templateUrl: './payments.page.html',
  styleUrl: '../../../../shared/styles/feature-page.css',
})
export class PaymentsPageComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  readonly pageKey = 'payments';
  readonly title = 'Payments';
  readonly description = 'Record incoming and outgoing payments for your customers and suppliers.';
  readonly indexPath = '/payments';
  readonly fields: Field[] = [
    { key: 'partyId', label: 'Customer or supplier', type: 'select', required: true, optionSource: 'parties' },
    { key: 'paymentNumber', label: 'Payment number', required: true },
    { key: 'direction', label: 'Direction', type: 'select', required: true, options: [
      { label: 'Incoming', value: 'Receive' },
      { label: 'Outgoing', value: 'Pay' },
    ] },
    { key: 'paymentDate', label: 'Payment date', type: 'date', required: true },
    { key: 'currencyCode', label: 'Currency code', type: 'select', required: true, optionSource: 'currencies' },
    { key: 'exchangeRate', label: 'Exchange rate', type: 'number', required: true },
    { key: 'amount', label: 'Amount', type: 'number', required: true },
  ];
  rows: Payment[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  form: Record<string, unknown> = {};
  options: Record<string, Choice[]> = {};
  postingFields: Field[] = [];
  formFields: Field[] = this.fields;
  loading = false;
  saving = false;
  error = '';
  view: 'index' | 'create' | 'edit' = 'index';
  formMode: RecordFormMode = 'create';
  editingRecordId: number | null = null;
  postingPaymentId: number | null = null;
  allocatingPaymentId: number | null = null;
  private routeSubscription?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly payments: PaymentService,
    private readonly business: BusinessService,
    private readonly optionData: OptionDataService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = combineLatest([this.route.data, this.route.paramMap]).subscribe(([data, params]) => {
      const view = data['view'];
      this.view = view === 'create' || view === 'edit' ? view : 'index';
      this.error = '';
      this.postingPaymentId = null;
      this.allocatingPaymentId = null;
      this.editingRecordId = toRecordId(params);
      if (this.view === 'create') {
        this.formMode = 'create';
        this.formFields = this.fields;
        this.form = { exchangeRate: 1 };
        this.loading = false;
        this.loadOptions(PAYMENT_OPTIONS);
      } else if (this.view === 'edit') {
        this.formMode = 'edit';
        this.formFields = this.fields;
        this.loadOptions(PAYMENT_OPTIONS);
        this.loadRecord();
      } else {
        this.loadRows();
      }
    });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
  }

  setFormValue(key: string, value: unknown): void {
    this.form[key] = value;
  }

  submit(): void {
    const partyId = Number(this.form['partyId']);
    const exchangeRate = Number(this.form['exchangeRate']);
    const amount = Number(this.form['amount']);
    const direction = this.form['direction'];
    if (!isPositiveId(partyId) || !String(this.form['paymentNumber'] ?? '').trim() ||
      (direction !== 'Receive' && direction !== 'Pay') || !String(this.form['paymentDate'] ?? '') ||
      !String(this.form['currencyCode'] ?? '').trim() || !Number.isFinite(exchangeRate) || exchangeRate <= 0 ||
      !Number.isFinite(amount) || amount <= 0) {
      this.error = 'Complete the payment details with a valid amount and exchange rate.';
      return;
    }

    const payload: PaymentPayload = {
      partyId,
      paymentNumber: String(this.form['paymentNumber']).trim(),
      direction,
      paymentDate: String(this.form['paymentDate']),
      currencyCode: String(this.form['currencyCode']),
      exchangeRate,
      amount,
    };
    if (this.view === 'edit' && this.editingRecordId === null) {
      this.error = 'A valid payment ID is required to update this record.';
      return;
    }
    const request = this.view === 'edit'
      ? this.payments.update(this.editingRecordId!, payload)
      : this.payments.create(payload);
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
        this.notifications.error(errorMessage(error, 'Could not save this payment.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  openPostForm(payment: Payment): void {
    if (payment.status === 'Posted') return;
    this.postingPaymentId = payment.id;
    this.allocatingPaymentId = null;
    this.formMode = 'post';
    this.formFields = [
      { key: 'journalId', label: 'Journal', type: 'select', required: true, optionSource: 'journals' },
      { key: 'accountingPeriodId', label: 'Accounting period', type: 'select', required: true, optionSource: 'periods' },
      { key: 'cashAccountId', label: 'Cash account', type: 'select', required: true, optionSource: 'accounts' },
      { key: 'controlAccountId', label: 'Control account', type: 'select', required: true, optionSource: 'accounts' },
    ];
    this.form = {};
    this.error = '';
    this.loadOptions(POSTING_OPTIONS);
  }

  openAllocationForm(payment: Payment): void {
    if (payment.status === 'Posted') return;
    this.allocatingPaymentId = payment.id;
    this.postingPaymentId = null;
    this.formMode = 'allocate';
    this.formFields = [
      { key: 'amount', label: 'Allocation amount', type: 'number', required: true },
      { key: 'documentType', label: 'Document type', type: 'select', required: true, options: [
        { label: 'Sales invoice', value: 'sales' },
        { label: 'Purchase bill', value: 'purchase' },
      ] },
      { key: 'documentId', label: 'Invoice or bill ID', type: 'number', required: true },
    ];
    this.form = {};
    this.error = '';
  }

  isEditable(payment: Payment): boolean {
    return payment.status === 'Draft' && Number(payment.allocatedAmount) === 0;
  }

  submitPost(): void {
    if (!this.postingPaymentId) return;
    const journalId = Number(this.form['journalId']);
    const accountingPeriodId = Number(this.form['accountingPeriodId']);
    const cashAccountId = Number(this.form['cashAccountId']);
    const controlAccountId = Number(this.form['controlAccountId']);
    if (![journalId, accountingPeriodId, cashAccountId, controlAccountId].every(isPositiveId)) {
      this.error = 'Choose a journal, accounting period, cash account, and control account.';
      return;
    }
    const payload: PaymentPostingPayload = { journalId, accountingPeriodId, cashAccountId, controlAccountId };
    this.runAction(
      this.business.postPayment(this.postingPaymentId, payload),
      'Payment posted successfully.',
      'Could not post this payment.',
    );
  }

  submitAllocation(): void {
    if (!this.allocatingPaymentId) return;
    const amount = Number(this.form['amount']);
    const documentType = this.form['documentType'];
    const documentId = Number(this.form['documentId']);
    if (!Number.isFinite(amount) || amount <= 0 || !isPositiveId(documentId) ||
      (documentType !== 'sales' && documentType !== 'purchase')) {
      this.error = 'Enter a positive allocation amount and select a valid invoice or bill.';
      return;
    }
    const payload: PaymentAllocationPayload = {
      amount,
      salesInvoiceId: documentType === 'sales' ? documentId : null,
      purchaseBillId: documentType === 'purchase' ? documentId : null,
    };
    this.runAction(
      this.business.allocatePayment(this.allocatingPaymentId, payload),
      'Payment allocated successfully.',
      'Could not save this payment allocation.',
    );
  }

  private runAction(
    request: ReturnType<BusinessService['postPayment']>,
    successMessage: string,
    failureMessage: string,
  ): void {
    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.postingPaymentId = null;
        this.allocatingPaymentId = null;
        this.saving = false;
        this.notifications.success(successMessage);
        this.loadRows();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, failureMessage));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRows(): void {
    this.loading = true;
    this.payments.getPage(this.page, this.pageSize).subscribe({
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
        this.notifications.error(errorMessage(error, 'Could not load payments.'));
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
    if (this.editingRecordId === null) {
      this.loading = false;
      this.error = 'A valid payment ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.payments.getById(this.editingRecordId).subscribe({
      next: payment => {
        if (payment.status !== 'Draft' || Number(payment.allocatedAmount) !== 0) {
          this.error = 'Only unallocated draft payments can be edited.';
          this.loading = false;
          this.changeDetector.markForCheck();
          return;
        }
        this.form = { ...payment };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load this payment.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadOptions(sources: string[]): void {
    this.options = {};
    for (const source of sources) {
      this.optionData.getOptions(source).subscribe({
        next: options => {
          this.options[source] = options;
          this.changeDetector.markForCheck();
        },
        error: error => {
          this.error = '';
          this.notifications.error(errorMessage(error, `Could not load ${source} options.`));
          this.changeDetector.markForCheck();
        },
      });
    }
  }
}

function toRecordId(params: ParamMap): number | null {
  const id = Number(params.get('recordId'));
  return isPositiveId(id) ? id : null;
}

function isPositiveId(value: number): boolean {
  return Number.isSafeInteger(value) && value > 0;
}
