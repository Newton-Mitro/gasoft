export interface Warehouse {
  id: number;
  code: string;
  name: string;
}

export interface WarehouseLocation {
  id: number;
  warehouseId: number;
  code: string;
  name: string;
}

export interface WarehousePayload {
  code: string;
  name: string;
}
