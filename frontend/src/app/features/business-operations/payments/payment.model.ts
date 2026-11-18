export interface Payment {
  id: number;
  partyId: number;
  paymentNumber: string;
  direction: string;
  paymentDate: string;
  currencyCode: string;
  exchangeRate: number | string;
  amount: number | string;
  allocatedAmount: number | string;
  status: string;
}

export interface PaymentPayload {
  partyId: number;
  paymentNumber: string;
  direction: 'Receive' | 'Pay';
  paymentDate: string;
  currencyCode: string;
  exchangeRate: number;
  amount: number;
}
