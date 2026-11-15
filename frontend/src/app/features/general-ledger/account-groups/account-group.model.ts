export type AccountType = 'Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense';

export interface AccountGroup {
  id: number;
  companyId: number;
  code: string;
  name: string;
  accountType: AccountType;
  normalBalance: string;
}

export interface AccountGroupPage {
  items: AccountGroup[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AccountGroupPayload {
  code: string;
  name: string;
  accountType: AccountType;
}
