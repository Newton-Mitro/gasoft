import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PurchaseOrdersService } from './purchase-orders.service';

describe('PurchaseOrdersService', () => {
  const purchaseOrder = {
    id: 2,
    companyId: 1,
    supplierId: 3,
    supplierName: 'Supplier',
    warehouseId: 4,
    orderNumber: 'PO-10',
    orderDate: '2026-11-16',
    expectedDate: null,
    currencyCode: 'USD',
    exchangeRate: 1,
    status: 'Ordered',
    revision: 1,
    lines: [{
      id: 5,
      itemId: 6,
      sku: 'SKU-6',
      itemName: 'Item 6',
      unitCode: 'EA',
      description: 'Item 6',
      quantity: 10,
      receivedQuantity: 0,
      unitCost: 2.5,
    }],
  };

  it('uses company-scoped purchase-order endpoints and passes revision/receipt details', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(endpoint === 'purchase-orders' ? [purchaseOrder] : purchaseOrder);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        if (endpoint.endsWith('/receive')) {
          return of({
            movement: {
              id: 9,
              movementNumber: 'GRN-PO-10-5',
              movementDate: '2026-11-16',
              quantityIn: 4,
              unitCost: 2.5,
              totalCost: 10,
            },
            purchaseOrderStatus: 'PartiallyReceived',
          });
        }
        return of(purchaseOrder);
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of(purchaseOrder);
      },
    } as unknown as ApiService;
    const service = new PurchaseOrdersService(api);

    service.getOrders().subscribe();
    service.getOrder(2).subscribe();
    service.create(purchaseOrderPayload).subscribe();
    service.update(2, { ...purchaseOrderPayload, revision: 1 }).subscribe();
    service.place(2, 1).subscribe();
    service.cancel(2, 1).subscribe();
    service.receive(2, 5, receiptPayload).subscribe();

    expect(calls.map(call => call.slice(0, 3))).toEqual([
      ['GET', 'purchase-orders', undefined],
      ['GET', 'purchase-orders/2', undefined],
      ['POST', 'purchase-orders', purchaseOrderPayload],
      ['PUT', 'purchase-orders/2', { ...purchaseOrderPayload, revision: 1 }],
      ['POST', 'purchase-orders/2/order', { revision: 1 }],
      ['POST', 'purchase-orders/2/cancel', { revision: 1 }],
      ['POST', 'purchase-orders/2/lines/5/receive', receiptPayload],
    ]);
  });

  it('rejects malformed purchase-order data', () => {
    const api = ({
      get: () => of([{ ...purchaseOrder, lines: [{ id: 0 }] }]),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new PurchaseOrdersService(api).getOrders().subscribe({
      error: error => receivedError = error,
    });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid records');
  });
});

const purchaseOrderPayload = {
  supplierId: 3,
  warehouseId: 4,
  orderNumber: 'PO-10',
  orderDate: '2026-11-16',
  expectedDate: null,
  currencyCode: 'USD',
  exchangeRate: 1,
  lines: [{ itemId: 6, description: 'Item 6', quantity: 10, unitCost: 2.5 }],
};

const receiptPayload = {
  movementNumber: 'GRN-PO-10-5',
  warehouseLocationId: null,
  journalId: 7,
  accountingPeriodId: 8,
  inventoryAccountId: 10,
  offsetAccountId: 11,
  movementDate: '2026-11-16',
  quantity: 4,
};
