import { TestBed } from '@angular/core/testing';
import { FormValidationService } from '../../../../shared/services/form-validation.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { InventoryBalance } from '../../../inventory/balances/inventory-balance.model';
import { Item } from '../../../inventory/items/item.model';
import { Party } from '../../parties/party.model';
import { PosPageComponent } from './pos.page';

describe('PosPageComponent checkout validation', () => {
  let validation: FormValidationService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: NotificationService,
          useValue: { success: vi.fn(), error: vi.fn() },
        },
      ],
    });
    validation = TestBed.inject(FormValidationService);
  });

  it('marks missing customer, cart, and required accounting selections', () => {
    const checkout = vi.fn();
    const page = createPage(checkout);

    page.checkout();

    expect(checkout).not.toHaveBeenCalled();
    expect(validation.messagesFor('partyId')).toEqual(['Select a customer.']);
    expect(validation.messagesFor('cart')).toEqual(['Add at least one item to the sale.']);
    expect(validation.messagesFor('warehouseId')).toEqual(['Select a warehouse.']);
  });

  it('rejects quantities above warehouse stock and invalid unit prices', () => {
    const checkout = vi.fn();
    const page = createPage(checkout);
    page.customers = [{ id: 7 } as Party];
    page.partyId = 7;
    page.warehouseId = 2;
    page.journalId = 3;
    page.accountingPeriodId = 4;
    page.revenueAccountId = 5;
    page.receivableAccountId = 6;
    page.settlementAccountId = 7;
    page.inventoryAccountId = 8;
    page.costOfGoodsSoldAccountId = 9;
    page.balances = [{ itemId: 11, warehouseId: 2, quantity: 2 } as InventoryBalance];
    page.cart = [{ item: { id: 11 } as Item, quantity: 3, unitPrice: -1 }];

    page.checkout();

    expect(checkout).not.toHaveBeenCalled();
    expect(validation.messagesFor('lines[0].quantity')).toEqual([
      'Quantity cannot exceed the 2 units available in the selected warehouse.',
    ]);
    expect(validation.messagesFor('lines[0].unitPrice')).toEqual([
      'Enter a valid unit price of zero or more.',
    ]);
  });

  function createPage(checkout: ReturnType<typeof vi.fn>): PosPageComponent {
    return TestBed.runInInjectionContext(
      () =>
        new PosPageComponent(
          {} as never,
          {} as never,
          {} as never,
          {} as never,
          {} as never,
          {} as never,
          { checkout } as never,
          { markForCheck: vi.fn() } as never,
        ),
    );
  }
});
