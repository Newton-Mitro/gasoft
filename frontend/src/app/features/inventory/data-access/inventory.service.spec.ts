import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { InventoryService } from './inventory.service';

describe('InventoryService', () => {
  it('records stock movements and posts counts through company-scoped endpoints', () => {
    const calls: unknown[][] = [];
    const api = {
      post: (endpoint: string, payload: unknown, companyScoped: boolean) => {
        calls.push([endpoint, payload, companyScoped]);
        return of({ id: 5 });
      },
    } as unknown as ApiService;
    const service = new InventoryService(api);
    const receipt = {
      movementNumber: 'R-1',
      itemId: 1,
      warehouseId: 2,
      warehouseLocationId: null,
      journalId: 3,
      accountingPeriodId: 4,
      inventoryAccountId: 5,
      movementDate: '2026-11-15',
      quantity: 6,
      offsetAccountId: 7,
      unitCost: 8,
    };
    const countPost = {
      journalId: 3,
      accountingPeriodId: 4,
      inventoryAccountId: 5,
      adjustmentAccountId: 6,
    };
    const issue = {
      movementNumber: 'I-1',
      itemId: 1,
      warehouseId: 2,
      warehouseLocationId: null,
      journalId: 3,
      accountingPeriodId: 4,
      inventoryAccountId: 5,
      movementDate: '2026-11-15',
      quantity: 2,
      costOfGoodsSoldAccountId: 6,
    };

    service.recordReceipt(receipt).subscribe();
    service.recordIssue(issue).subscribe();
    service.postCount(10, countPost).subscribe();

    expect(calls).toEqual([
      ['inventory/receipts', receipt, true],
      ['inventory/issues', issue, true],
      ['inventory/counts/10/post', countPost, true],
    ]);
  });
});
