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

export function isIsoDateOnly(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const date = new Date(`${value}T00:00:00.000Z`);
  return Number.isFinite(date.getTime()) && date.toISOString().slice(0, 10) === value;
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
