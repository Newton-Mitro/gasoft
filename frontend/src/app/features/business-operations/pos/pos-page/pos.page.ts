import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../../../core/api.service';
import { AuthService } from '../../../../core/auth.service';
import { InventoryBalanceService } from '../../../inventory/balances/inventory-balance.service';
import { InventoryBalance } from '../../../inventory/balances/inventory-balance.model';
import { Item } from '../../../inventory/items/item.model';
import { ItemService } from '../../../inventory/items/item.service';
import { Choice } from '../../../../shared/models/shared.models';
import { FieldValidationDirective } from '../../../../shared/directives/field-validation.directive';
import { SearchableSelectDirective } from '../../../../shared/directives/searchable-select.directive';
import {
  FormValidationErrors,
  FormValidationService,
} from '../../../../shared/services/form-validation.service';
import { OptionDataService } from '../../../../shared/data/option-data.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { Party } from '../../parties/party.model';
import { PartyService } from '../../parties/party.service';
import { PosSalePayload, PosSaleResult, PosSettings } from '../pos.model';
import { PosService } from '../pos.service';

interface CompanyOption {
  id: number;
  functionalCurrencyCode: string;
  legalName: string;
}

interface CartLine {
  item: Item;
  quantity: number;
  unitPrice: number | null;
}

@Component({
  selector: 'app-pos-page',
  imports: [FormsModule, FieldValidationDirective, SearchableSelectDirective],
  templateUrl: './pos.page.html',
  styleUrl: './pos.page.css',
})
export class PosPageComponent implements OnInit {
  readonly notifications = inject(NotificationService);
  private readonly formValidation = inject(FormValidationService);
  readonly title = 'Point of sale';
  items: Item[] = [];
  customers: Party[] = [];
  walkInCustomer: Party | null = null;
  balances: InventoryBalance[] = [];
  options: Record<string, Choice[]> = {};
  cart: CartLine[] = [];
  search = '';
  loading = true;
  saving = false;
  error = '';
  functionalCurrencyCode = '';
  companyName = '';
  lastSale: PosSaleResult | null = null;
  partyId: number | null = null;
  warehouseId: number | null = null;
  journalId: number | null = null;
  accountingPeriodId: number | null = null;
  revenueAccountId: number | null = null;
  receivableAccountId: number | null = null;
  taxCodeId: number | null = null;
  taxAccountId: number | null = null;
  settlementAccountId: number | null = null;
  inventoryAccountId: number | null = null;
  costOfGoodsSoldAccountId: number | null = null;

  constructor(
    private readonly api: ApiService,
    readonly auth: AuthService,
    private readonly itemsApi: ItemService,
    private readonly balancesApi: InventoryBalanceService,
    private readonly partiesApi: PartyService,
    private readonly optionData: OptionDataService,
    private readonly pos: PosService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    const companyId = this.auth.companyId();
    if (!companyId) {
      this.loading = false;
      this.error = 'Choose an organization before opening the point of sale.';
      return;
    }

    forkJoin({
      items: this.itemsApi.getAll(),
      balances: this.balancesApi.getAll(),
      parties: this.partiesApi.getAll(),
      walkInCustomer: this.partiesApi.getWalkInCustomer(),
      companies: this.api.get<CompanyOption[]>('/api/v1/tenant/companies', false),
      warehouses: this.optionData.getOptions('warehouses'),
      journals: this.optionData.getOptions('journals'),
      periods: this.optionData.getOptions('periods'),
      revenueAccounts: this.optionData.getAccountOptionsByType('Revenue'),
      assetAccounts: this.optionData.getAccountOptionsByType('Asset'),
      taxAccounts: this.optionData.getAccountOptionsByType('Liability'),
      expenseAccounts: this.optionData.getAccountOptionsByType('Expense'),
      taxCodes: this.optionData.getOptions('taxCodes'),
      posSettings: this.pos.getSettings(),
    }).subscribe({
      next: (result) => {
        this.items = result.items;
        this.balances = result.balances;
        this.walkInCustomer = result.walkInCustomer;
        this.customers = result.parties.filter(
          (party) => party.isCustomer && party.id !== this.walkInCustomer?.id,
        );
        const company = result.companies.find((option) => option.id === companyId);
        if (!company) {
          this.error = 'The selected organization could not be found.';
          this.loading = false;
          this.changeDetector.markForCheck();
          return;
        }
        this.functionalCurrencyCode = company.functionalCurrencyCode;
        this.companyName = company.legalName;
        this.options = {
          warehouses: result.warehouses,
          journals: result.journals,
          periods: result.periods.filter((option) => option.label.endsWith('· Open')),
          revenueAccounts: result.revenueAccounts,
          assetAccounts: result.assetAccounts,
          taxAccounts: result.taxAccounts,
          expenseAccounts: result.expenseAccounts,
          taxCodes: result.taxCodes,
        };
        this.setSetupValues(result.posSettings);
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: (error) => {
        this.loading = false;
        this.error = errorMessage(error, 'Could not load POS setup data.');
        this.changeDetector.markForCheck();
      },
    });
  }

  get filteredItems(): Item[] {
    const query = this.search.trim().toLocaleLowerCase();
    return this.items
      .filter(
        (item) =>
          !query ||
          item.sku.toLocaleLowerCase().includes(query) ||
          item.name.toLocaleLowerCase().includes(query),
      )
      .slice(0, 60);
  }

  get subtotal(): number {
    return this.cart.reduce((total, line) => total + line.quantity * (line.unitPrice ?? 0), 0);
  }

  get setupComplete(): boolean {
    return (
      this.hasOption('warehouses', this.warehouseId) &&
      this.hasOption('journals', this.journalId) &&
      this.hasOption('periods', this.accountingPeriodId) &&
      this.hasOption('revenueAccounts', this.revenueAccountId) &&
      this.hasOption('assetAccounts', this.receivableAccountId) &&
      this.hasOption('assetAccounts', this.settlementAccountId) &&
      this.hasOption('assetAccounts', this.inventoryAccountId) &&
      this.hasOption('expenseAccounts', this.costOfGoodsSoldAccountId) &&
      (this.taxCodeId === null ||
        (this.hasOption('taxCodes', this.taxCodeId) &&
          this.hasOption('taxAccounts', this.taxAccountId)))
    );
  }

  get canCheckout(): boolean {
    return (
      this.cart.length > 0 &&
      this.setupComplete &&
      this.hasCustomerSelection(this.partyId) &&
      this.cart.every(
        (line) =>
          Number.isFinite(Number(line.quantity)) &&
          Number(line.quantity) > 0 &&
          Number(line.quantity) <= this.availableQuantity(line.item.id) &&
          this.hasSellingPrice(line.item) &&
          line.unitPrice !== null &&
          Number.isFinite(Number(line.unitPrice)) &&
          Number(line.unitPrice) >= 0,
      )
    );
  }

  addItem(item: Item): void {
    if (item.unitSellingPrice === null) {
      this.notifications.error(`Set a selling price for ${item.name} in Inventory items before selling it.`);
      return;
    }
    if (item.sellingPriceCurrencyCode !== this.functionalCurrencyCode) {
      this.notifications.error(
        `Update the selling price for ${item.name} to the selected company's functional currency before selling it.`,
      );
      return;
    }

    const available = this.availableQuantity(item.id);
    if (available <= 0) {
      this.notifications.error(`${item.name} is out of stock in the selected warehouse.`);
      return;
    }

    const line = this.cart.find((candidate) => candidate.item.id === item.id);
    if (line && line.quantity + 1 > available) {
      this.notifications.error(
        `Only ${available} ${item.baseUnitCode} available for ${item.name}.`,
      );
      return;
    }
    if (line) line.quantity += 1;
    else this.cart = [...this.cart, { item, quantity: 1, unitPrice: item.unitSellingPrice }];
    this.formValidation.clear();
    this.lastSale = null;
  }

  adjustQuantity(itemId: number, change: number): void {
    const line = this.cart.find((candidate) => candidate.item.id === itemId);
    if (!line) return;
    const quantity = Number(line.quantity) + change;
    if (Number.isFinite(quantity) && quantity > 0) {
      line.quantity = quantity;
      const index = this.cart.indexOf(line);
      this.formValidation.clearField(`lines[${index}].quantity`);
    }
  }

  addFirstMatch(): void {
    const item = this.filteredItems[0];
    if (item) {
      this.addItem(item);
      this.search = '';
    }
  }

  removeItem(itemId: number): void {
    this.cart = this.cart.filter((line) => line.item.id !== itemId);
    this.formValidation.clear();
  }

  clearCart(): void {
    this.cart = [];
    this.formValidation.clear();
  }

  availableQuantity(itemId: number): number {
    return this.balances
      .filter(
        (balance) =>
          balance.itemId === itemId &&
          (this.warehouseId === null || balance.warehouseId === this.warehouseId),
      )
      .reduce((total, balance) => total + Number(balance.quantity), 0);
  }

  hasSellingPrice(item: Item): boolean {
    return (
      item.unitSellingPrice !== null &&
      item.sellingPriceCurrencyCode === this.functionalCurrencyCode
    );
  }

  formatMoney(value: number | string, currencyCode = this.functionalCurrencyCode): string {
    const amount = Number(value);
    if (!Number.isFinite(amount) || !currencyCode) return String(value);
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: currencyCode,
    }).format(amount);
  }

  fieldMessages(field: string): string[] {
    return this.formValidation.messagesFor(field);
  }

  clearFieldError(field: string): void {
    this.formValidation.clearField(field);
  }

  checkout(): void {
    this.formValidation.clear();
    const validationErrors = this.validateCheckout();
    this.formValidation.setErrors(validationErrors);
    if (Object.keys(validationErrors).length) {
      return;
    }

    const saleDate = localDate();
    const reference = createReference();
    const taxCodeId = this.taxCodeId || null;
    const payload: PosSalePayload = {
      partyId: this.partyId!,
      invoiceNumber: `POS-${reference}`,
      paymentNumber: `POS-PAY-${reference}`,
      saleDate,
      currencyCode: this.functionalCurrencyCode,
      exchangeRate: 1,
      lines: this.cart.map((line) => ({
        itemId: line.item.id,
        description: `${line.item.sku} · ${line.item.name}`,
        revenueAccountId: this.revenueAccountId!,
        taxCodeId,
        quantity: Number(line.quantity),
        unitPrice: Number(line.unitPrice),
      })),
      warehouseId: this.warehouseId!,
      journalId: this.journalId!,
      accountingPeriodId: this.accountingPeriodId!,
      receivableAccountId: this.receivableAccountId!,
      taxAccounts: taxCodeId ? [{ taxCodeId, accountId: this.taxAccountId! }] : [],
      settlementAccountId: this.settlementAccountId!,
      inventoryAccountId: this.inventoryAccountId!,
      costOfGoodsSoldAccountId: this.costOfGoodsSoldAccountId!,
    };

    this.saving = true;
    this.error = '';
    this.formValidation.clear();
    this.pos.checkout(payload).subscribe({
      next: (result) => {
        this.lastSale = result;
        this.cart = [];
        this.search = '';
        this.saving = false;
        this.notifications.success(`Sale ${result.invoice.invoiceNumber} completed.`);
        this.balancesApi.getAll().subscribe({
          next: (balances) => {
            this.balances = balances;
            this.changeDetector.markForCheck();
          },
          error: (error) => {
            this.notifications.error(
              errorMessage(error, 'Sale completed, but stock balances could not be refreshed.'),
            );
            this.changeDetector.markForCheck();
          },
        });
        this.changeDetector.markForCheck();
      },
      error: (error) => {
        this.saving = false;
        const details = errorMessage(
          error,
          'The sale could not be completed. No sale records were committed.',
        );
        const validationErrors = serverValidationErrors(error);
        if (Object.keys(validationErrors).length) {
          this.formValidation.setErrors(validationErrors);
          this.error = Object.keys(this.formValidation.unmatchedErrors()).length ? details : '';
        } else {
          this.error = details;
        }
        this.changeDetector.markForCheck();
      },
    });
  }

  printReceipt(): void {
    window.print();
  }

  private validateCheckout(): FormValidationErrors {
    const errors: FormValidationErrors = {};
    if (!this.hasCustomerSelection(this.partyId)) {
      errors['partyId'] = ['Select a customer.'];
    }
    if (!this.cart.length) errors['cart'] = ['Add at least one item to the sale.'];

    const requiredSelections: Array<[string, number | null, string, Choice[]]> = [
      ['warehouseId', this.warehouseId, 'Select a warehouse.', this.options['warehouses'] ?? []],
      ['journalId', this.journalId, 'Select a journal.', this.options['journals'] ?? []],
      [
        'accountingPeriodId',
        this.accountingPeriodId,
        'Select an open accounting period.',
        this.options['periods'] ?? [],
      ],
      [
        'revenueAccountId',
        this.revenueAccountId,
        'Select a sales revenue account.',
        this.options['revenueAccounts'] ?? [],
      ],
      [
        'receivableAccountId',
        this.receivableAccountId,
        'Select a receivable account.',
        this.options['assetAccounts'] ?? [],
      ],
      [
        'settlementAccountId',
        this.settlementAccountId,
        'Select a cash or card account.',
        this.options['assetAccounts'] ?? [],
      ],
      [
        'inventoryAccountId',
        this.inventoryAccountId,
        'Select an inventory asset account.',
        this.options['assetAccounts'] ?? [],
      ],
      [
        'costOfGoodsSoldAccountId',
        this.costOfGoodsSoldAccountId,
        'Select a cost of goods sold account.',
        this.options['expenseAccounts'] ?? [],
      ],
    ];
    for (const [field, value, message, options] of requiredSelections) {
      if (!isPositiveId(value) || !options.some((option) => Number(option.value) === value)) {
        errors[field] = [message];
      }
    }

    if (this.taxCodeId !== null) {
      if (
        !isPositiveId(this.taxCodeId) ||
        !(this.options['taxCodes'] ?? []).some((option) => Number(option.value) === this.taxCodeId)
      ) {
        errors['taxCodeId'] = ['Select a valid tax code.'];
      }
      if (
        !isPositiveId(this.taxAccountId) ||
        !(this.options['taxAccounts'] ?? []).some(
          (option) => Number(option.value) === this.taxAccountId,
        )
      ) {
        errors['taxAccountId'] = ['Select a tax liability account.'];
      }
    }

    this.cart.forEach((line, index) => {
      if (!Number.isFinite(Number(line.quantity)) || Number(line.quantity) <= 0) {
        errors[`lines[${index}].quantity`] = ['Quantity must be greater than zero.'];
      } else {
        const available = this.availableQuantity(line.item.id);
        if (Number(line.quantity) > available) {
          errors[`lines[${index}].quantity`] = [
            `Quantity cannot exceed the ${available} units available in the selected warehouse.`,
          ];
        }
      }
      if (
        line.item.unitSellingPrice === null ||
        line.item.sellingPriceCurrencyCode !== this.functionalCurrencyCode
      ) {
        errors[`lines[${index}].unitPrice`] = [
          'Configure a selling price in the selected company currency before checkout.',
        ];
      } else if (
        line.unitPrice === null ||
        !Number.isFinite(Number(line.unitPrice)) ||
        Number(line.unitPrice) < 0
      ) {
        errors[`lines[${index}].unitPrice`] = ['Enter a valid unit price of zero or more.'];
      }
    });

    return errors;
  }

  private hasOption(source: string, value: number | null): boolean {
    return (
      isPositiveId(value) &&
      (this.options[source] ?? []).some((option) => Number(option.value) === value)
    );
  }

  private hasCustomerSelection(value: number | null): boolean {
    return isPositiveId(value) &&
      (this.walkInCustomer?.id === value || this.customers.some((customer) => customer.id === value));
  }

  private setSetupValues(settings: PosSettings): void {
    this.warehouseId = settings.warehouseId;
    this.journalId = settings.journalId;
    this.accountingPeriodId = settings.accountingPeriodId;
    this.revenueAccountId = settings.revenueAccountId;
    this.receivableAccountId = settings.receivableAccountId;
    this.settlementAccountId = settings.settlementAccountId;
    this.taxCodeId = settings.taxCodeId;
    this.taxAccountId = settings.taxAccountId;
    this.inventoryAccountId = settings.inventoryAccountId;
    this.costOfGoodsSoldAccountId = settings.costOfGoodsSoldAccountId;
  }
}

function isPositiveId(value: number | null): boolean {
  return value !== null && Number.isSafeInteger(value) && value > 0;
}

function localDate(): string {
  const date = new Date();
  const offset = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offset).toISOString().slice(0, 10);
}

function createReference(): string {
  return `${localDate().replace(/-/g, '')}-${Math.random().toString(36).slice(2, 8).toUpperCase()}`;
}

function serverValidationErrors(error: unknown): FormValidationErrors {
  if (
    !(error instanceof HttpErrorResponse) ||
    (error.status !== 400 && error.status !== 422) ||
    !error.error ||
    typeof error.error !== 'object' ||
    !('errors' in error.error) ||
    !error.error.errors ||
    typeof error.error.errors !== 'object'
  ) {
    return {};
  }

  return Object.fromEntries(
    Object.entries(error.error.errors).flatMap(([field, messages]) => {
      const fieldMessages = Array.isArray(messages)
        ? messages.filter(
            (message): message is string => typeof message === 'string' && !!message.trim(),
          )
        : typeof messages === 'string' && messages.trim()
          ? [messages]
          : [];
      return fieldMessages.length ? [[field, fieldMessages]] : [];
    }),
  );
}
