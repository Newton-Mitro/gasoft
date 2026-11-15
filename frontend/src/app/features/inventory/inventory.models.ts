export interface InventoryMovementBase {
  movementNumber: string;
  itemId: number;
  warehouseId: number;
  warehouseLocationId: null;
  journalId: number;
  accountingPeriodId: number;
  inventoryAccountId: number;
  movementDate: string;
  quantity: number;
}

export interface InventoryReceiptPayload extends InventoryMovementBase {
  offsetAccountId: number;
  unitCost: number;
}

export interface InventoryIssuePayload extends InventoryMovementBase {
  costOfGoodsSoldAccountId: number;
}

export interface InventoryCountPostingPayload {
  journalId: number;
  accountingPeriodId: number;
  inventoryAccountId: number;
  adjustmentAccountId: number;
}

export interface WarehouseLocationPayload {
  code: string;
  name: string;
}
