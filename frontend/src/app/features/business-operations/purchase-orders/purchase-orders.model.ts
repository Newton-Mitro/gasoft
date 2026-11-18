export interface PurchaseOrderLine {
  id: number;
  itemId: number;
  sku: string;
  itemName: string;
  unitCode: string;
  description: string;
  quantity: number;
  receivedQuantity: number;
  unitCost: number;
}

export interface PurchaseOrder {
  id: number;
  companyId: number;
  supplierId: number;
  supplierName: string;
  warehouseId: number;
  orderNumber: string;
  orderDate: string;
  expectedDate: string | null;
  currencyCode: string;
  exchangeRate: number;
  status: string;
  revision: number;
  lines: PurchaseOrderLine[];
}

export interface PurchaseOrderLinePayload {
  itemId: number;
  description: string;
  quantity: number;
  unitCost: number;
}

export interface PurchaseOrderPayload {
  supplierId: number;
  warehouseId: number;
  orderNumber: string;
  orderDate: string;
  expectedDate: string | null;
  currencyCode: string;
  exchangeRate: number;
  lines: PurchaseOrderLinePayload[];
  revision?: number;
}

export interface ReceivePurchaseOrderLinePayload {
  movementNumber: string;
  warehouseLocationId: number | null;
  journalId: number;
  accountingPeriodId: number;
  inventoryAccountId: number;
  offsetAccountId: number;
  movementDate: string;
  quantity: number;
}

export interface PurchaseOrderReceipt {
  movement: {
    id: number;
    movementNumber: string;
    movementDate: string;
    quantityIn: number;
    unitCost: number;
    totalCost: number;
  };
  purchaseOrderStatus: string;
}
