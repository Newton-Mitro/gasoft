export interface Choice {
  label: string;
  value: string | number;
}

export interface Field {
  key: string;
  label: string;
  type?: 'text' | 'password' | 'email' | 'tel' | 'url' | 'date' | 'number' | 'checkbox' | 'select';
  required?: boolean;
  min?: number;
  max?: number;
  minLength?: number;
  maxLength?: number;
  pattern?: string;
  step?: number | 'any';
  options?: Choice[];
  optionSource?: 'groups' | 'years' | 'parties' | 'accounts' | 'categories' | 'taxCategories' | 'currencies' | 'journals' | 'periods' | 'warehouses' | 'items' | 'units' | 'jurisdictions' | 'taxCodes';
}

export type DataRow = Record<string, unknown>;

export interface DocumentLineForm {
  accountId: number | null;
  taxCodeId: number | null;
  description: string;
  quantity: number;
  unitPrice: number;
}

export interface JournalEntryLineForm {
  accountId: number | null;
  debit: number | null;
  credit: number | null;
}

export type RecordFormMode = 'create' | 'edit' | 'post' | 'allocate' | 'receive' | 'issue' | 'countPost' | 'location';
