export interface TaxAccountMapping {
  taxCodeId: number;
  accountId: number;
}

interface DocumentPostingBase {
  journalId: number;
  accountingPeriodId: number;
}

export interface SalesInvoicePostingPayload extends DocumentPostingBase {
  receivableAccountId: number;
  taxAccounts: TaxAccountMapping[];
}

export interface PurchaseBillPostingPayload extends DocumentPostingBase {
  payableAccountId: number;
  taxAccounts: TaxAccountMapping[];
}

export interface PaymentPostingPayload extends DocumentPostingBase {
  cashAccountId: number;
  controlAccountId: number;
}

export interface PaymentAllocationPayload {
  amount: number;
  salesInvoiceId: number | null;
  purchaseBillId: number | null;
}
