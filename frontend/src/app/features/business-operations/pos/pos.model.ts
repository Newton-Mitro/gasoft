export interface PosSaleLinePayload {
  itemId: number;
  description: string;
  revenueAccountId: number;
  taxCodeId: number | null;
  quantity: number;
  unitPrice: number;
}

export interface PosSalePayload {
  partyId: number;
  invoiceNumber: string;
  paymentNumber: string;
  saleDate: string;
  currencyCode: string;
  exchangeRate: number;
  lines: PosSaleLinePayload[];
  warehouseId: number;
  journalId: number;
  accountingPeriodId: number;
  receivableAccountId: number;
  taxAccounts: Array<{ taxCodeId: number; accountId: number }>;
  settlementAccountId: number;
  inventoryAccountId: number;
  costOfGoodsSoldAccountId: number;
}

export interface PosSettings {
  warehouseId: number | null;
  journalId: number | null;
  accountingPeriodId: number | null;
  revenueAccountId: number | null;
  receivableAccountId: number | null;
  settlementAccountId: number | null;
  taxCodeId: number | null;
  taxAccountId: number | null;
  inventoryAccountId: number | null;
  costOfGoodsSoldAccountId: number | null;
}

export interface PosSaleResult {
  invoice: {
    id: number;
    invoiceNumber: string;
    grandTotal: number | string;
    currencyCode: string;
    lines: Array<{
      description: string;
      quantity: number | string;
      grossAmount: number | string;
    }>;
  };
  payment: {
    id: number;
    paymentNumber: string;
    status: string;
  };
}
