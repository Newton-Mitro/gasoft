export type CostingMethod = 'FIFO' | 'AVCO';

export interface Item {
  id: number;
  categoryId: number;
  sku: string;
  name: string;
  baseUnitCode: string;
  costingMethod: CostingMethod;
}

export interface ItemPayload {
  categoryId: number;
  sku: string;
  name: string;
  baseUnitCode: string;
  costingMethod: CostingMethod;
}
