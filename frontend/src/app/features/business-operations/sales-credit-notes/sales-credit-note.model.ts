export interface SalesCreditNoteLine {
  id: number;
  salesInvoiceLineId: number;
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

export interface SalesCreditNote {
  id: number;
  salesInvoiceId: number;
  invoiceNumber: string;
  creditNoteNumber: string;
  creditNoteDate: string;
  reason: string;
  currencyCode: string;
  netTotal: number | string;
  taxTotal: number | string;
  grandTotal: number | string;
  status: string;
  refundedAmount: number | string;
  refundableAmount: number | string;
  lines: SalesCreditNoteLine[];
}

export interface SalesCreditNotePayload {
  salesInvoiceId: number;
  creditNoteNumber: string;
  creditNoteDate: string;
  reason: string;
  lines: Array<{ salesInvoiceLineId: number; quantity: number }>;
}

export interface SalesCreditNotePostingPayload {
  journalId: number;
  accountingPeriodId: number;
  receivableAccountId: number;
  taxAccounts: Array<{ taxCodeId: number; accountId: number }>;
}

export interface CustomerRefundPayload {
  refundNumber: string;
  refundDate: string;
  amount: number;
  journalId: number;
  accountingPeriodId: number;
  cashAccountId: number;
}
