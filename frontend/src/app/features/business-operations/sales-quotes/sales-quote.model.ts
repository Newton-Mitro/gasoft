export interface SalesQuoteLine {
  id: number;
  accountId: number;
  taxCodeId: number | null;
  description: string;
  quantity: number | string;
  unitPrice: number | string;
  netAmount: number | string;
  taxRatePercent: number | string;
  taxAmount: number | string;
  grossAmount: number | string;
}

export interface SalesQuote {
  id: number;
  customerId: number;
  salesInvoiceId: number | null;
  quoteNumber: string;
  quoteDate: string;
  validUntil: string;
  currencyCode: string;
  exchangeRate: number | string;
  netTotal: number | string;
  taxTotal: number | string;
  grandTotal: number | string;
  status: string;
  sentAt: string | null;
  sentTo: string | null;
  lines: SalesQuoteLine[];
}

export interface SalesQuotePayload {
  customerId: number;
  quoteNumber: string;
  quoteDate: string;
  validUntil: string;
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
