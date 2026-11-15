export interface TaxRegistration {
  id: number;
  companyId: number;
  jurisdictionId: number;
  code: string;
  countryCode: string;
  registrationNumber: string;
}

export interface TaxRegistrationPayload {
  jurisdictionId: number;
  registrationNumber: string;
}

export interface TaxRegistrationMutation {
  id: number;
  companyId: number;
  jurisdictionId: number;
  registrationNumber: string;
}
