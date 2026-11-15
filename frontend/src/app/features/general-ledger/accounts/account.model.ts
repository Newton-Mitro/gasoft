export interface Account {
  id: number;
  companyId: number;
  accountGroupId: number;
  parentId: number | null;
  code: string;
  name: string;
  allowPosting: boolean;
  isActive: boolean;
}

export interface AccountPage {
  items: Account[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  totalAccounts: number;
}

export interface AccountPayload {
  accountGroupId: number;
  code: string;
  name: string;
  parentId: number | null;
  allowPosting: boolean;
  isActive?: boolean;
}
