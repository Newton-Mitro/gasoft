export interface FiscalYear {
  id: number;
  startsOn: string;
  endsOn: string;
  status: string;
}

export interface FiscalYearPayload {
  startsOn: string;
  endsOn: string;
}

export interface FiscalYearCloseChecklist {
  fiscalYearId: number;
  accountingPeriodCount: number;
  openAccountingPeriodCount: number;
  draftJournalEntryCount: number;
  closingJournalRequired: boolean;
  closingJournalEntryId: number | null;
  closingJournalStatus: string | null;
  closingJournalLines: FiscalYearClosingLine[];
  canClose: boolean;
}

export interface FiscalYearClosingLine {
  accountCode: string;
  accountName: string;
  debit: number;
  credit: number;
}

export interface FiscalYearStatusChange {
  id: number;
  fromStatus: string;
  toStatus: string;
  reason: string;
  changedAt: string;
  changedByUserId: number;
  changedByDisplayName: string;
}
