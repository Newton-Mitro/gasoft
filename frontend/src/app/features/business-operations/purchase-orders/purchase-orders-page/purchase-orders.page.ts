import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../../../core/api.service';
import { AuthService } from '../../../../core/auth.service';
import { CompanyService } from '../../../settings/company/company.service';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { Choice } from '../../../../shared/models/shared.models';
import { PageChange, PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { OptionDataService } from '../../../../shared/data/option-data.service';
import {
  PurchaseOrder,
  PurchaseOrderLinePayload,
  PurchaseOrderPayload,
  ReceivePurchaseOrderLinePayload,
} from '../purchase-orders.model';
import { PurchaseOrdersService } from '../purchase-orders.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { FieldValidationDirective } from '../../../../shared/directives/field-validation.directive';
import { SearchableSelectDirective } from '../../../../shared/directives/searchable-select.directive';
import { InputComponent } from '../../../../shared/components/input/input.component';
import {
  FormValidationErrors,
  FormValidationService,
} from '../../../../shared/services/form-validation.service';

interface PartyOption {
  id: number;
  partyNo: string;
  displayName: string;
  isSupplier: boolean;
}

interface ItemOption {
  id: number;
  sku: string;
  name: string;
  baseUnitCode: string;
}

interface EditableLine {
  itemId: number | null;
  description: string;
  quantity: number | null;
  unitCost: number | null;
}

@Component({
  selector: 'app-purchase-orders-page',
  imports: [FormsModule, PaginationComponent, FieldValidationDirective, SearchableSelectDirective, InputComponent],
  templateUrl: './purchase-orders.page.html',
  styleUrls: ['../../../../shared/styles/feature-page.css', './purchase-orders.page.css'],
})
export class PurchaseOrdersPageComponent implements OnInit {
  readonly notifications = inject(NotificationService);
  private readonly formValidation = inject(FormValidationService);
  orders: PurchaseOrder[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  suppliers: PartyOption[] = [];
  items: ItemOption[] = [];
  warehouses: Choice[] = [];
  journals: Choice[] = [];
  periods: Choice[] = [];
  accounts: Choice[] = [];
  currencies: Choice[] = [];
  functionalCurrency = '';
  selectedOrderId: number | null = null;
  editing = false;
  saving = false;
  loading = true;
  error = '';
  success = '';
  supplierId: number | null = null;
  warehouseId: number | null = null;
  orderNumber = '';
  orderDate = new Date().toISOString().slice(0, 10);
  expectedDate = '';
  currencyCode = '';
  exchangeRate = 1;
  revision = 0;
  lines: EditableLine[] = [];
  receiveLineId: number | null = null;
  receiveMovementNumber = '';
  receiveDate = new Date().toISOString().slice(0, 10);
  receiveQuantity: number | null = null;
  receiveLocationId: number | null = null;
  receiveJournalId: number | null = null;
  receivePeriodId: number | null = null;
  receiveInventoryAccountId: number | null = null;
  receiveOffsetAccountId: number | null = null;

  constructor(
    private readonly service: PurchaseOrdersService,
    private readonly options: OptionDataService,
    private readonly api: ApiService,
    private readonly auth: AuthService,
    private readonly company: CompanyService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    const companyId = this.auth.companyId();
    if (!companyId) {
      this.error = 'Choose a company before managing purchase orders.';
      this.loading = false;
      return;
    }
    forkJoin({
      parties: this.api.get<unknown>('/api/v1/parties/', false),
      items: this.api.get<unknown>('/api/v1/items', false),
      warehouses: this.options.getOptions('warehouses'),
      journals: this.options.getOptions('journals'),
      periods: this.options.getOptions('periods'),
      accounts: this.options.getOptions('accounts'),
      currencies: this.options.getOptions('currencies'),
      company: this.company.getById(companyId),
      orders: this.service.getOrdersPage(this.page, this.pageSize),
    }).subscribe({
      next: result => {
        this.suppliers = parseSuppliers(result.parties);
        this.items = parseItems(result.items);
        this.warehouses = result.warehouses;
        this.journals = result.journals;
        this.periods = result.periods;
        this.accounts = result.accounts;
        this.currencies = result.currencies;
        this.functionalCurrency = result.company.functionalCurrencyCode;
        this.orders = result.orders.items;
        this.page = result.orders.page;
        this.pageSize = result.orders.pageSize;
        this.totalCount = result.orders.totalCount;
        this.totalPages = result.orders.totalPages;
        this.currencyCode = this.functionalCurrency;
        this.selectFirstOrder();
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load purchase order data.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  get selectedOrder(): PurchaseOrder | undefined {
    return this.orders.find(order => order.id === this.selectedOrderId);
  }

  get remainingTotal(): number {
    return this.lines.reduce((sum, line) => sum + (line.quantity ?? 0) * (line.unitCost ?? 0), 0);
  }

  formatAmount(value: number, currency = this.selectedOrder?.currencyCode ?? this.currencyCode): string {
    if (!Number.isFinite(value) || !/^[A-Z]{3}$/.test(currency)) return '—';
    return new Intl.NumberFormat(undefined, { style: 'currency', currency, maximumFractionDigits: 2 }).format(value);
  }

  warehouseLabel(warehouseId: number): string {
    return this.warehouses.find(option => Number(option.value) === warehouseId)?.label ?? String(warehouseId);
  }

  orderTotal(order: PurchaseOrder): number {
    return order.lines.reduce((sum, line) => sum + line.quantity * line.unitCost, 0);
  }

  beginCreate(): void {
    this.formValidation.clear();
    this.selectedOrderId = null;
    this.editing = true;
    this.error = '';
    this.success = '';
    this.supplierId = null;
    this.warehouseId = null;
    this.orderNumber = '';
    this.orderDate = new Date().toISOString().slice(0, 10);
    this.expectedDate = '';
    this.currencyCode = this.functionalCurrency;
    this.exchangeRate = 1;
    this.revision = 0;
    this.lines = [{ itemId: null, description: '', quantity: 1, unitCost: 0 }];
    this.receiveLineId = null;
  }

  editOrder(order: PurchaseOrder): void {
    if (order.status !== 'Draft') return;
    this.formValidation.clear();
    this.selectedOrderId = order.id;
    this.editing = true;
    this.error = '';
    this.supplierId = order.supplierId;
    this.warehouseId = order.warehouseId;
    this.orderNumber = order.orderNumber;
    this.orderDate = order.orderDate;
    this.expectedDate = order.expectedDate ?? '';
    this.currencyCode = order.currencyCode;
    this.exchangeRate = order.exchangeRate;
    this.revision = order.revision;
    this.lines = order.lines.map(line => ({
      itemId: line.itemId,
      description: line.description,
      quantity: line.quantity,
      unitCost: line.unitCost,
    }));
    this.receiveLineId = null;
  }

  cancelEdit(): void {
    this.formValidation.clear();
    this.editing = false;
    this.error = '';
    this.selectFirstOrder();
  }

  addLine(): void {
    this.formValidation.clear();
    this.lines = [...this.lines, { itemId: null, description: '', quantity: 1, unitCost: 0 }];
  }

  removeLine(index: number): void {
    if (this.lines.length > 1) {
      this.formValidation.clear();
      this.lines = this.lines.filter((_, position) => position !== index);
    }
  }

  selectItem(index: number, value: number | string): void {
    const itemId = Number(value);
    const item = this.items.find(option => option.id === itemId);
    if (!item) return;
    this.formValidation.clearField(`description-${index}`);
    this.lines = this.lines.map((line, position) => position === index
      ? { ...line, itemId, description: item.name }
      : line);
  }

  saveOrder(): void {
    this.formValidation.clear();
    const validationErrors = validateOrder(this);
    this.formValidation.setErrors(validationErrors);
    if (Object.keys(validationErrors).length) {
      return;
    }
    const orderLines: PurchaseOrderLinePayload[] = [];
    for (const line of this.lines) {
      if (!isPositiveId(line.itemId) || line.quantity === null || line.unitCost === null) return;
      orderLines.push({
        itemId: line.itemId,
        description: line.description.trim(),
        quantity: line.quantity,
        unitCost: line.unitCost,
      });
    }
    const supplierId = this.supplierId;
    const warehouseId = this.warehouseId;
    if (!isPositiveId(supplierId) || !isPositiveId(warehouseId)) return;
    const payload: PurchaseOrderPayload = {
      supplierId,
      warehouseId,
      orderNumber: this.orderNumber.trim(),
      orderDate: this.orderDate,
      expectedDate: this.expectedDate || null,
      currencyCode: this.currencyCode,
      exchangeRate: this.exchangeRate,
      lines: orderLines,
      revision: this.revision,
    };
    this.saving = true;
    this.error = '';
    const request = this.selectedOrderId
      ? this.service.update(this.selectedOrderId, payload)
      : this.service.create(payload);
    request.subscribe({
      next: order => {
        this.upsertOrder(order);
        this.selectedOrderId = order.id;
        this.editing = false;
        this.saving = false;
        this.formValidation.clear();
        this.notifications.success(`Purchase order ${order.orderNumber} saved as a draft.`);
        this.success = '';
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.showServerValidationErrors(error, 'Could not save the purchase order.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  placeOrder(order: PurchaseOrder): void {
    this.saving = true;
    this.error = '';
    this.service.place(order.id, order.revision).subscribe({
      next: updated => {
        this.upsertOrder(updated);
        this.notifications.success(`Purchase order ${updated.orderNumber} is now open for receiving.`);
        this.success = '';
        this.saving = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not place this purchase order.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  cancelOrder(order: PurchaseOrder): void {
    this.saving = true;
    this.error = '';
    this.service.cancel(order.id, order.revision).subscribe({
      next: () => {
        this.service.getOrder(order.id).subscribe({
          next: updated => {
            this.upsertOrder(updated);
            this.notifications.success(`Purchase order ${updated.orderNumber} cancelled.`);
            this.success = '';
            this.saving = false;
            this.changeDetector.markForCheck();
          },
          error: error => {
            this.error = '';
            this.notifications.error(errorMessage(error, 'The order was cancelled, but its updated details could not be loaded.'));
            this.saving = false;
            this.changeDetector.markForCheck();
          },
        });
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not cancel this purchase order.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  beginReceive(order: PurchaseOrder, lineId: number): void {
    const line = order.lines.find(item => item.id === lineId);
    if (!line || !['Ordered', 'PartiallyReceived'].includes(order.status)) return;
    this.formValidation.clear();
    this.selectedOrderId = order.id;
    this.receiveLineId = lineId;
    this.receiveMovementNumber = `GRN-${order.orderNumber}-${line.id}-${Date.now().toString().slice(-5)}`.slice(0, 40);
    this.receiveDate = new Date().toISOString().slice(0, 10);
    this.receiveQuantity = Number((line.quantity - line.receivedQuantity).toFixed(6));
    this.receiveLocationId = null;
    this.receiveJournalId = null;
    this.receivePeriodId = null;
    this.receiveInventoryAccountId = null;
    this.receiveOffsetAccountId = null;
    this.error = '';
  }

  receiveLine(order: PurchaseOrder): void {
    this.formValidation.clear();
    const validationErrors = validateReceipt(this, order);
    this.formValidation.setErrors(validationErrors);
    if (Object.keys(validationErrors).length) {
      return;
    }
    const receiveLineId = this.receiveLineId;
    const receiveQuantity = this.receiveQuantity;
    const receiveJournalId = this.receiveJournalId;
    const receivePeriodId = this.receivePeriodId;
    const receiveInventoryAccountId = this.receiveInventoryAccountId;
    const receiveOffsetAccountId = this.receiveOffsetAccountId;
    if (
      receiveLineId === null ||
      receiveQuantity === null ||
      receiveJournalId === null ||
      receivePeriodId === null ||
      receiveInventoryAccountId === null ||
      receiveOffsetAccountId === null
    ) return;
    const payload: ReceivePurchaseOrderLinePayload = {
      movementNumber: this.receiveMovementNumber.trim(),
      warehouseLocationId: this.receiveLocationId,
      journalId: receiveJournalId,
      accountingPeriodId: receivePeriodId,
      inventoryAccountId: receiveInventoryAccountId,
      offsetAccountId: receiveOffsetAccountId,
      movementDate: this.receiveDate,
      quantity: receiveQuantity,
    };
    this.saving = true;
    this.error = '';
    this.service.receive(order.id, receiveLineId, payload).subscribe({
      next: result => {
        this.notifications.success(`Received ${result.movement.quantityIn} units. Inventory movement ${result.movement.movementNumber} was posted.`);
        this.success = '';
        this.receiveLineId = null;
        this.formValidation.clear();
        this.reloadOrder(order.id);
      },
      error: error => {
        this.error = '';
        this.showServerValidationErrors(error, 'Could not receive this purchase order line.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  cancelReceive(): void {
    this.formValidation.clear();
    this.receiveLineId = null;
    this.error = '';
  }

  private reloadOrder(orderId: number): void {
    this.service.getOrder(orderId).subscribe({
      next: order => {
        this.upsertOrder(order);
        this.saving = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'The receipt posted, but the updated purchase order could not be loaded.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private selectFirstOrder(): void {
    if (!this.selectedOrderId && this.orders.length) this.selectedOrderId = this.orders[0].id;
  }

  onPageChange(change: PageChange): void {
    this.page = change.page;
    this.pageSize = change.pageSize;
    this.loading = true;
    this.receiveLineId = null;
    this.service.getOrdersPage(this.page, this.pageSize).subscribe({
      next: result => {
        this.orders = result.items;
        this.page = result.page;
        this.pageSize = result.pageSize;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.selectedOrderId = null;
        this.selectFirstOrder();
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load this page of purchase orders.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private upsertOrder(order: PurchaseOrder): void {
    this.orders = [order, ...this.orders.filter(item => item.id !== order.id)]
      .sort((a, b) => b.orderDate.localeCompare(a.orderDate) || b.id - a.id);
  }

  private showServerValidationErrors(error: unknown, fallback: string): void {
    const validationErrors = serverValidationErrors(error);
    if (!Object.keys(validationErrors).length) {
      this.notifications.error(errorMessage(error, fallback));
      return;
    }

    this.formValidation.setErrors(validationErrors);
    if (Object.keys(this.formValidation.unmatchedErrors()).length) {
      this.notifications.error(errorMessage(error, fallback));
    }
  }
}

function parseSuppliers(value: unknown): PartyOption[] {
  if (!Array.isArray(value)) throw new Error('Supplier data returned an invalid response.');
  return value.filter(isSupplier);
}

function isSupplier(value: unknown): value is PartyOption {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return typeof row['id'] === 'number' &&
    typeof row['partyNo'] === 'string' &&
    typeof row['displayName'] === 'string' &&
    typeof row['isSupplier'] === 'boolean' &&
    row['isSupplier'];
}

function parseItems(value: unknown): ItemOption[] {
  if (!Array.isArray(value) || !value.every(isItem)) throw new Error('Item data returned an invalid response.');
  return value;
}

function isItem(value: unknown): value is ItemOption {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return typeof row['id'] === 'number' &&
    typeof row['sku'] === 'string' &&
    typeof row['name'] === 'string' &&
    typeof row['baseUnitCode'] === 'string';
}

function validateOrder(page: PurchaseOrdersPageComponent): FormValidationErrors {
  const errors: FormValidationErrors = {};
  if (!isPositiveId(page.supplierId)) errors['supplier'] = ['Select a supplier.'];
  if (!isPositiveId(page.warehouseId)) errors['warehouse'] = ['Select a warehouse.'];
  if (!page.orderNumber.trim()) errors['orderNumber'] = ['Enter an order number.'];
  else if (page.orderNumber.trim().length > 40) {
    errors['orderNumber'] = ['Order number must be 40 characters or fewer.'];
  }
  if (!isDateOnly(page.orderDate)) errors['orderDate'] = ['Enter a valid order date.'];
  if (page.expectedDate) {
    if (!isDateOnly(page.expectedDate)) {
      errors['expectedDate'] = ['Enter a valid expected date.'];
    } else if (isDateOnly(page.orderDate) && page.expectedDate < page.orderDate) {
      errors['expectedDate'] = ['Expected date must be on or after the order date.'];
    }
  }
  if (!/^[A-Z]{3}$/.test(page.currencyCode)) errors['currency'] = ['Select a currency.'];
  if (!Number.isFinite(page.exchangeRate) || page.exchangeRate <= 0) {
    errors['exchangeRate'] = ['Exchange rate must be greater than zero.'];
  }
  if (!page.lines.length) errors['item-0'] = ['Add at least one order item.'];
  page.lines.forEach((line, index) => {
    if (!isPositiveId(line.itemId)) errors[`item-${index}`] = ['Select an inventory item.'];
    if (!line.description.trim()) errors[`description-${index}`] = ['Enter a line description.'];
    else if (line.description.trim().length > 500) {
      errors[`description-${index}`] = ['Description must be 500 characters or fewer.'];
    }
    if (line.quantity === null || !Number.isFinite(line.quantity) || line.quantity <= 0) {
      errors[`quantity-${index}`] = ['Quantity must be greater than zero.'];
    }
    if (line.unitCost === null || !Number.isFinite(line.unitCost) || line.unitCost <= 0) {
      errors[`unit-cost-${index}`] = ['Unit cost must be greater than zero.'];
    }
  });
  return errors;
}

function validateReceipt(
  page: PurchaseOrdersPageComponent,
  order: PurchaseOrder,
): FormValidationErrors {
  const errors: FormValidationErrors = {};
  if (!page.receiveMovementNumber.trim()) {
    errors['receiveMovementNumber'] = ['Enter a movement number.'];
  } else if (page.receiveMovementNumber.trim().length > 40) {
    errors['receiveMovementNumber'] = ['Movement number must be 40 characters or fewer.'];
  }
  if (!isDateOnly(page.receiveDate)) errors['receiveDate'] = ['Enter a valid receipt date.'];
  const orderLine = order.lines.find(line => line.id === page.receiveLineId);
  const remaining = orderLine ? orderLine.quantity - orderLine.receivedQuantity : 0;
  if (page.receiveQuantity === null || !Number.isFinite(page.receiveQuantity) || page.receiveQuantity <= 0) {
    errors['receiveQuantity'] = ['Quantity must be greater than zero.'];
  } else if (page.receiveQuantity > remaining) {
    errors['receiveQuantity'] = [`Quantity cannot exceed the remaining ${remaining} units.`];
  }
  if (!isPositiveId(page.receiveJournalId)) errors['receiveJournal'] = ['Select a journal.'];
  if (!isPositiveId(page.receivePeriodId)) errors['receivePeriod'] = ['Select an accounting period.'];
  if (!isPositiveId(page.receiveInventoryAccountId)) {
    errors['inventoryAccount'] = ['Select an inventory asset account.'];
  }
  if (!isPositiveId(page.receiveOffsetAccountId)) {
    errors['offsetAccount'] = ['Select an offset account.'];
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

function isPositiveId(value: number | null): value is number {
  return Number.isSafeInteger(value) && value !== null && value > 0;
}

function isDateOnly(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const date = new Date(`${value}T00:00:00.000Z`);
  return !Number.isNaN(date.valueOf()) && date.toISOString().slice(0, 10) === value;
}
