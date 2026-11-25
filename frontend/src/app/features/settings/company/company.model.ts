export interface Company {
  id: number;
  code: string;
  legalName: string;
  functionalCurrencyCode: string;
  phone: string | null;
  email: string | null;
  website: string | null;
  address: string | null;
  cityRegion: string | null;
  postalCode: string | null;
  country: string | null;
  taxId: string | null;
  logoUrl: string | null;
}

export interface UpdateCompanyPayload {
  legalName: string;
  functionalCurrencyCode: string;
  phone: string;
  email: string;
  website: string;
  address: string;
  cityRegion: string;
  postalCode: string;
  country: string;
  taxId: string;
}

export interface CreateCompanyPayload {
  code: string;
  legalName: string;
  functionalCurrencyCode: string;
  phone: string;
  email: string;
  website: string;
  address: string;
  cityRegion: string;
  postalCode: string;
  country: string;
  taxId: string;
}
