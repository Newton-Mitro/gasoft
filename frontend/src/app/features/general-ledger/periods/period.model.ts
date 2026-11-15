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
