export interface Company {
  id: number;
  code: string;
  legalName: string;
  functionalCurrencyCode: string;
}

export interface UpdateCompanyPayload {
  legalName: string;
  functionalCurrencyCode: string;
}
