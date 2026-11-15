export interface InventoryCountItem {
  itemId: number;
  countedQuantity: number | string;
  adjustmentUnitCost: number | string | null;
}

export interface InventoryCount {
  id: number;
  countNumber: string;
  countDate: string;
  warehouseId: number;
  status: string;
  items: InventoryCountItem[];
}

export interface InventoryCountPayload {
  warehouseId: number;
  countNumber: string;
  countDate: string;
  items: InventoryCountItem[];
}
