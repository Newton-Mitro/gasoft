export interface ExpenseReportLine {
  id: number;
  expenseDate: string;
  merchant: string;
  description: string;
  accountId: number;
  amount: number;
  receiptUrl: string | null;
}

export interface ExpenseReport {
  id: number;
  companyId: number;
  employeeUserId: number;
  employeeName: string;
  reportNumber: string;
  title: string;
  currencyCode: string;
  totalAmount: number;
  status: string;
  reviewNote: string | null;
  submittedAt: string | null;
  reviewedAt: string | null;
  postedAt: string | null;
  reimbursedAt: string | null;
  journalEntryId: number | null;
  reimbursementJournalEntryId: number | null;
  canEdit: boolean;
  canSubmit: boolean;
  canApprove: boolean;
  canPost: boolean;
  canReimburse: boolean;
  lines: ExpenseReportLine[];
}

export interface ExpenseReportPayload {
  title: string;
  lines: Array<{
    id?: number;
    expenseDate: string;
    merchant: string;
    description: string;
    accountId: number;
    amount: number;
  }>;
}

export interface AccountingPeriodOption {
  id: number;
  startsOn: string;
  endsOn: string;
  status: string;
}
