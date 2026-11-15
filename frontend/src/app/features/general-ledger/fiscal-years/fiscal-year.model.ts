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
