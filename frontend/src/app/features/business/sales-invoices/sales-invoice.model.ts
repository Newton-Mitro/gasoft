export interface SalesInvoiceLine {
  id?: number;
  accountId: number;
  taxCodeId: number | null;
  description: string;
  quantity: number | string;
  unitPrice: number | string;
}

export interface SalesInvoice {
  id: number;
  partyId: number;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  currencyCode: string;
  exchangeRate: number | string;
  grandTotal: number | string;
  allocatedTotal: number | string;
  status: string;
  lines?: SalesInvoiceLine[];
}

export interface SalesInvoicePayload {
  partyId: number;
  invoiceNumber: string;
  invoiceDate: string;
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
