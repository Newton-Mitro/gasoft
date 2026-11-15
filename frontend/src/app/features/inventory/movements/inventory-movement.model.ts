export interface InventoryMovement {
  id: number;
  movementNumber: string;
  movementType: string;
  movementDate: string;
  itemId: number | string;
  warehouseId: number | string;
  quantityIn: number | string;
  quantityOut: number | string;
  totalCost: number | string;
}
