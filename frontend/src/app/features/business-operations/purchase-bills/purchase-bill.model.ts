export interface PurchaseBillLine {
  id?: number;
  accountId: number;
  taxCodeId: number | null;
  description: string;
  quantity: number | string;
  unitPrice: number | string;
}

export interface PurchaseBill {
  id: number;
  partyId: number;
  billNumber: string;
  billDate: string;
  dueDate: string;
  currencyCode: string;
  exchangeRate: number | string;
  grandTotal: number | string;
  allocatedTotal: number | string;
  status: string;
  lines?: PurchaseBillLine[];
}

export interface PurchaseBillPayload {
  partyId: number;
  billNumber: string;
  billDate: string;
  dueDate: string;
  currencyCode: string;
  exchangeRate: number;
  lines: Array<{
    accountId: number;
    taxCodeId: number | null;
    description: string;
    quantity: number;
    unitPrice: number;
  }>;
}
