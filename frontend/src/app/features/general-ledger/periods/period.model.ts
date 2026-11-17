export interface AccountingPeriod {
  id: number;
  fiscalYearId: number;
  startsOn: string;
  endsOn: string;
  status: string;
}

export interface AccountingPeriodPayload {
  fiscalYearId: number;
  startsOn: string;
  endsOn: string;
}

export interface AccountingPeriodStatusChange {
  id: number;
  fromStatus: string;
  toStatus: string;
  reason: string;
  changedAt: string;
  changedByUserId: number;
  changedByDisplayName: string;
}

export interface AccountingPeriodCloseChecklist {
  periodId: number;
  draftJournalEntryCount: number;
  unreconciledBankTransactionCount: number;
  canClose: boolean;
}
