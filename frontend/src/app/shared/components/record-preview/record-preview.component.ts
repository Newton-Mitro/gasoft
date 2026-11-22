import { Component, Input } from '@angular/core';
import { Choice, Field, RecordFormMode } from '../../models/shared.models';

interface PreviewConfiguration {
  title: string;
  eyebrow: string;
  icon: string;
  primaryField: string;
  detailFields: string[];
  description?: string;
  primarySuffixField?: string;
  formatPrimaryAmount?: boolean;
}

const PREVIEW_CONFIGURATIONS: Record<string, PreviewConfiguration> = {
  accounts: {
    title: 'Account preview',
    eyebrow: 'CHART OF ACCOUNTS',
    icon: 'fa-landmark',
    primaryField: 'name',
    detailFields: ['code', 'accountGroupId', 'parentId', 'allowPosting', 'isActive'],
    description: 'General ledger account',
  },
  'account-groups': {
    title: 'Account group preview',
    eyebrow: 'CHART OF ACCOUNTS',
    icon: 'fa-layer-group',
    primaryField: 'name',
    detailFields: ['code', 'accountType'],
    description: 'Financial statement grouping',
  },
  'fiscal-years': {
    title: 'Fiscal year preview',
    eyebrow: 'GENERAL LEDGER',
    icon: 'fa-calendar-days',
    primaryField: 'startsOn',
    detailFields: ['endsOn'],
    description: 'Fiscal year date range',
  },
  periods: {
    title: 'Accounting period preview',
    eyebrow: 'GENERAL LEDGER',
    icon: 'fa-calendar-check',
    primaryField: 'startsOn',
    detailFields: ['fiscalYearId', 'endsOn'],
    description: 'Period date range',
  },
  journals: {
    title: 'Journal preview',
    eyebrow: 'GENERAL LEDGER',
    icon: 'fa-book',
    primaryField: 'name',
    detailFields: ['code', 'journalType', 'isActive'],
    description: 'Journal configuration',
  },
  parties: {
    title: 'Customer or supplier preview',
    eyebrow: 'CUSTOMERS & SUPPLIERS',
    icon: 'fa-address-card',
    primaryField: 'displayName',
    detailFields: ['partyNo', 'isCustomer', 'isSupplier', 'email', 'phone', 'defaultCurrencyCode'],
    description: 'Party record',
  },
  'party-addresses': {
    title: 'Address preview',
    eyebrow: 'CUSTOMER OR SUPPLIER',
    icon: 'fa-location-dot',
    primaryField: 'line1',
    detailFields: ['addressType', 'line2', 'city', 'region', 'postalCode', 'countryCode', 'isDefault'],
    description: 'Contact address',
  },
  payments: {
    title: 'Payment preview',
    eyebrow: 'PAYMENT SUMMARY',
    icon: 'fa-credit-card',
    primaryField: 'amount',
    detailFields: ['paymentNumber', 'partyId', 'direction', 'paymentDate', 'currencyCode', 'exchangeRate'],
    primarySuffixField: 'currencyCode',
    formatPrimaryAmount: true,
    description: 'Payment amount',
  },
  'bank-accounts': {
    title: 'Bank account preview',
    eyebrow: 'CASH MANAGEMENT',
    icon: 'fa-building-columns',
    primaryField: 'name',
    detailFields: ['bankName', 'lastFour', 'chartOfAccountId', 'currencyCode'],
    description: 'Bank reconciliation account',
  },
  items: {
    title: 'Inventory item preview',
    eyebrow: 'INVENTORY',
    icon: 'fa-box',
    primaryField: 'name',
    detailFields: ['sku', 'categoryId', 'baseUnitCode', 'costingMethod'],
    description: 'Stock item',
  },
  'item-categories': {
    title: 'Item category preview',
    eyebrow: 'INVENTORY',
    icon: 'fa-tags',
    primaryField: 'name',
    detailFields: ['code'],
    description: 'Inventory classification',
  },
  warehouses: {
    title: 'Warehouse preview',
    eyebrow: 'INVENTORY',
    icon: 'fa-warehouse',
    primaryField: 'name',
    detailFields: ['code'],
    description: 'Stock location',
  },
  'inventory-counts': {
    title: 'Inventory count preview',
    eyebrow: 'INVENTORY',
    icon: 'fa-clipboard-check',
    primaryField: 'countNumber',
    detailFields: ['warehouseId', 'countDate', 'itemId', 'countedQuantity', 'adjustmentUnitCost'],
    description: 'Physical stock count',
  },
  'tax-codes': {
    title: 'Tax code preview',
    eyebrow: 'TAX SETTINGS',
    icon: 'fa-percent',
    primaryField: 'code',
    detailFields: ['jurisdictionId', 'categoryId', 'taxType'],
    description: 'Tax treatment',
  },
  'tax-registrations': {
    title: 'Tax registration preview',
    eyebrow: 'TAX SETTINGS',
    icon: 'fa-file-invoice',
    primaryField: 'registrationNumber',
    detailFields: ['jurisdictionId'],
    description: 'Organization registration',
  },
  company: {
    title: 'Company profile preview',
    eyebrow: 'COMPANY PROFILE',
    icon: 'fa-building',
    primaryField: 'legalName',
    detailFields: ['functionalCurrencyCode', 'phone', 'email', 'website', 'cityRegion', 'taxId'],
    description: 'Organization details',
  },
  companies: {
    title: 'Organization preview',
    eyebrow: 'COMPANY PROFILE',
    icon: 'fa-building',
    primaryField: 'legalName',
    detailFields: ['code', 'functionalCurrencyCode'],
    description: 'New organization',
  },
  users: {
    title: 'Team member preview',
    eyebrow: 'WORKSPACE ACCESS',
    icon: 'fa-user-shield',
    primaryField: 'displayName',
    detailFields: ['email', 'role', 'isActive'],
    description: 'Workspace membership',
  },
};

const MAX_PREVIEW_FIELDS = 8;

export function shouldShowRecordPreview(pageKey: string, formMode: RecordFormMode, fields: Field[]): boolean {
  return (formMode === 'create' || formMode === 'edit') &&
    fields.length > 0 &&
    fields.length <= MAX_PREVIEW_FIELDS &&
    Object.hasOwn(PREVIEW_CONFIGURATIONS, pageKey);
}

@Component({
  selector: 'app-record-preview',
  templateUrl: './record-preview.component.html',
  styleUrl: './record-preview.component.css',
})
export class RecordPreviewComponent {
  @Input() pageKey = '';
  @Input() formMode: RecordFormMode = 'create';
  @Input() fields: Field[] = [];
  @Input() form: Record<string, unknown> = {};
  @Input() options: Record<string, Choice[]> = {};

  get configuration(): PreviewConfiguration {
    return PREVIEW_CONFIGURATIONS[this.pageKey] ?? PREVIEW_CONFIGURATIONS['accounts'];
  }

  get primaryField(): Field | undefined {
    return this.fields.find(field => field.key === this.configuration.primaryField)
      ?? this.fields.find(field => field.type !== 'password');
  }

  get detailFields(): Field[] {
    const primaryKey = this.primaryField?.key;
    return this.configuration.detailFields
      .map(key => this.fields.find(field => field.key === key))
      .filter((field): field is Field => !!field && field.key !== primaryKey && field.type !== 'password');
  }

  get primaryLabel(): string {
    if (this.pageKey === 'payments') return 'Payment amount';
    return this.primaryField?.label ?? 'Record';
  }

  get primaryValue(): string {
    const field = this.primaryField;
    if (!field) return 'Not set';
    const value = this.form[field.key];
    if (this.configuration.formatPrimaryAmount && value !== null && value !== undefined && String(value).trim()) {
      const amount = Number(value);
      if (Number.isFinite(amount)) {
        return amount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
      }
    }
    return this.formatFieldValue(field);
  }

  get primarySuffix(): string {
    const key = this.configuration.primarySuffixField;
    const field = key ? this.fields.find(candidate => candidate.key === key) : undefined;
    if (key === 'currencyCode') return String(this.form[key] ?? '').trim() || 'Not set';
    return field ? this.formatFieldValue(field) : '';
  }

  get statusLabel(): string {
    return this.formMode === 'edit' ? 'Editing' : 'Draft';
  }

  formatFieldValue(field: Field): string {
    const value = this.form[field.key];
    if (value === null || value === undefined || value === '') return 'Not set';
    if (field.type === 'checkbox') {
      if (field.key === 'isActive') return value ? 'Active' : 'Inactive';
      if (field.key === 'allowPosting') return value ? 'Allowed' : 'Not allowed';
      if (field.key === 'isDefault') return value ? 'Default' : 'Standard';
      return value ? 'Yes' : 'No';
    }
    if (field.type === 'select') {
      const choices = field.options ?? this.options[field.optionSource ?? ''] ?? [];
      return choices.find(choice => String(choice.value) === String(value))?.label ?? String(value);
    }
    if (field.type === 'number') {
      const number = Number(value);
      return Number.isFinite(number) ? number.toLocaleString() : String(value);
    }
    return String(value);
  }

  get iconClass(): string {
    return this.configuration.icon;
  }
}
