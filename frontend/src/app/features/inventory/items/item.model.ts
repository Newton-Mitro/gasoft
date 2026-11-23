export type CostingMethod = 'FIFO' | 'AVCO';

export interface Item {
  id: number;
  categoryId: number;
  sku: string;
  name: string;
  baseUnitCode: string;
  costingMethod: CostingMethod;
  unitSellingPrice: number | null;
  sellingPriceCurrencyCode: string | null;
}

export interface ItemPayload {
  categoryId: number;
  sku: string;
  name: string;
  baseUnitCode: string;
  costingMethod: CostingMethod;
  unitSellingPrice: number | null;
}
