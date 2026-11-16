export interface TaxRate {
  id: number;
  taxCodeId: number;
  ratePercent: number;
  validFrom: string;
  validTo: string | null;
}

export interface TaxCode {
  id: number;
  jurisdictionId: number;
  jurisdictionCode: string;
  countryCode: string;
  categoryId: number;
  categoryCode: string;
  categoryName: string;
  code: string;
  taxType: string;
  rates: TaxRate[];
}

export interface TaxCodePayload {
  jurisdictionId: number;
  categoryId: number;
  code: string;
  taxType: string;
}

export interface TaxRatePayload {
  ratePercent: number;
  validFrom: string;
  validTo: string | null;
}
