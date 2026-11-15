export type PartyAddressType = 'Billing' | 'Shipping' | 'Other';

export interface PartyAddress {
  id: number;
  addressType: PartyAddressType;
  line1: string;
  line2: string | null;
  city: string;
  region: string | null;
  postalCode: string | null;
  countryCode: string;
  isDefault: boolean;
}

export interface PartyAddressPayload {
  addressType: PartyAddressType;
  line1: string;
  line2: string | null;
  city: string;
  region: string | null;
  postalCode: string | null;
  countryCode: string;
  isDefault: boolean;
}
