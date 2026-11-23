import { NEVER } from 'rxjs';
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

  it('accepts a walk-in customer for checkout', () => {
    const page = createPage(vi.fn());
    page.walkInCustomer = { id: 19, isCustomer: true } as Party;
    page.partyId = 19;

    page.checkout();

    expect(validation.messagesFor('partyId')).toEqual([]);
  });

  it('prefills the configured company selling price when adding an item', () => {
    const page = createPage(vi.fn());
    page.functionalCurrencyCode = 'USD';
    page.balances = [{ itemId: 11, warehouseId: 2, quantity: 3 } as InventoryBalance];
    const item = {
      id: 11,
      sku: 'SKU-11',
      name: 'Item',
      unitSellingPrice: 12.5,
      sellingPriceCurrencyCode: 'USD',
    } as Item;

    page.addItem(item);

    expect(page.cart).toEqual([{ item, quantity: 1, unitPrice: 12.5 }]);
  });

  it('does not add an item without a configured selling price', () => {
    const page = createPage(vi.fn());
    page.functionalCurrencyCode = 'USD';
    const item = {
      id: 11,
      name: 'Item',
      unitSellingPrice: null,
      sellingPriceCurrencyCode: 'USD',
    } as Item;

    page.addItem(item);

    expect(page.cart).toEqual([]);
    expect(page.notifications.error).toHaveBeenCalledWith(
      'Set a selling price for Item in Inventory items before selling it.',
    );
  });

  it('uses the register selections when building a checkout request', () => {
    const checkout = vi.fn(() => NEVER);
    const page = createPage(checkout);
    page.walkInCustomer = { id: 19, isCustomer: true } as Party;
    page.partyId = 19;
    page.warehouseId = 2;
    page.journalId = 3;
    page.accountingPeriodId = 4;
    page.revenueAccountId = 5;
    page.receivableAccountId = 6;
    page.settlementAccountId = 7;
    page.inventoryAccountId = 8;
    page.costOfGoodsSoldAccountId = 9;
    page.options = {
      warehouses: [{ value: 2, label: 'Main warehouse' }],
      journals: [{ value: 3, label: 'Sales journal' }],
      periods: [{ value: 4, label: 'Open period' }],
      revenueAccounts: [{ value: 5, label: 'Sales revenue' }],
      assetAccounts: [
        { value: 6, label: 'Receivables' },
        { value: 7, label: 'Cash' },
        { value: 8, label: 'Inventory' },
      ],
      expenseAccounts: [{ value: 9, label: 'Cost of goods sold' }],
      taxCodes: [],
    };
    page.balances = [{ itemId: 11, warehouseId: 2, quantity: 3 } as InventoryBalance];
    page.cart = [{
      item: {
        id: 11,
        sku: 'SKU-11',
        name: 'Item',
        unitSellingPrice: 12,
        sellingPriceCurrencyCode: 'USD',
      } as Item,
      quantity: 1,
      unitPrice: 12,
    }];
    page.functionalCurrencyCode = 'USD';

    page.checkout();

    expect(checkout).toHaveBeenCalledWith(expect.objectContaining({
      warehouseId: 2,
      journalId: 3,
      accountingPeriodId: 4,
      settlementAccountId: 7,
    }));
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
    page.functionalCurrencyCode = 'USD';
    page.cart = [{
      item: {
        id: 11,
        unitSellingPrice: 12,
        sellingPriceCurrencyCode: 'USD',
      } as Item,
      quantity: 3,
      unitPrice: -1,
    }];

    page.checkout();

    expect(checkout).not.toHaveBeenCalled();
    expect(validation.messagesFor('lines[0].quantity')).toEqual([
      'Quantity cannot exceed the 2 units available in the selected warehouse.',
    ]);
    expect(validation.messagesFor('lines[0].unitPrice')).toEqual([
      'Enter a valid unit price of zero or more.',
    ]);
  });

  it('rejects a cart item without a configured company selling price', () => {
    const checkout = vi.fn();
    const page = createPage(checkout);
    page.customers = [{ id: 7 } as Party];
    page.partyId = 7;
    page.functionalCurrencyCode = 'USD';
    page.cart = [{
      item: {
        id: 11,
        unitSellingPrice: null,
        sellingPriceCurrencyCode: 'USD',
      } as Item,
      quantity: 1,
      unitPrice: 10,
    }];

    page.checkout();

    expect(checkout).not.toHaveBeenCalled();
    expect(validation.messagesFor('lines[0].unitPrice')).toEqual([
      'Configure a selling price in the selected company currency before checkout.',
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
