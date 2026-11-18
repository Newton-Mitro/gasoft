export interface Party {
  id: number;
  partyNo: string;
  displayName: string;
  isCustomer: boolean;
  isSupplier: boolean;
  email: string | null;
  phone: string | null;
  defaultCurrencyCode: string | null;
}

export interface PartyPayload {
  partyNo: string;
  displayName: string;
  isCustomer: boolean;
  isSupplier: boolean;
  email: string | null;
  phone: string | null;
  defaultCurrencyCode: string | null;
}
