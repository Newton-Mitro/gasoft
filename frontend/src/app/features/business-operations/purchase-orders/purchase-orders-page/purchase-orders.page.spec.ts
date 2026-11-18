import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ApiService } from '../../../../core/api.service';
import { AuthService } from '../../../../core/auth.service';
import { CompanyService } from '../../../settings/company/company.service';
import { FormValidationService } from '../../../../shared/services/form-validation.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { PurchaseOrdersService } from '../purchase-orders.service';
import { PurchaseOrdersPageComponent } from './purchase-orders.page';

describe('PurchaseOrdersPageComponent validation', () => {
  function createPage(serviceOverrides: Partial<PurchaseOrdersService> = {}) {
    TestBed.configureTestingModule({
      providers: [
        FormValidationService,
        { provide: NotificationService, useValue: { success: vi.fn(), error: vi.fn() } },
      ],
    });
    const service = {
      create: vi.fn(() => of({})),
      update: vi.fn(() => of({})),
      receive: vi.fn(() => of({ movement: { quantityIn: 1, movementNumber: 'GRN-1' } })),
      ...serviceOverrides,
    } as unknown as PurchaseOrdersService;
    const page = TestBed.runInInjectionContext(
      () =>
        new PurchaseOrdersPageComponent(
          service,
          {} as never,
          {} as ApiService,
          {} as AuthService,
          {} as CompanyService,
          { markForCheck: vi.fn() } as unknown as ChangeDetectorRef,
        ),
    );
    return { page, service, validation: TestBed.inject(FormValidationService) };
  }

  it('shows field-level order errors and skips saving invalid values', () => {
    const { page, service, validation } = createPage();
    page.lines = [{ itemId: null, description: '', quantity: 0, unitCost: 0 }];

    page.saveOrder();

    expect(service.create).not.toHaveBeenCalled();
    expect(validation.messagesFor('supplier')).toEqual(['Select a supplier.']);
    expect(validation.messagesFor('warehouse')).toEqual(['Select a warehouse.']);
    expect(validation.messagesFor('orderNumber')).toEqual(['Enter an order number.']);
    expect(validation.messagesFor('item-0')).toEqual(['Select an inventory item.']);
    expect(validation.messagesFor('description-0')).toEqual(['Enter a line description.']);
    expect(validation.messagesFor('quantity-0')).toEqual(['Quantity must be greater than zero.']);
    expect(validation.messagesFor('unit-cost-0')).toEqual(['Unit cost must be greater than zero.']);
  });

  it('shows a server field validation error beside its matching order field', () => {
    const apiError = new HttpErrorResponse({
      status: 400,
      error: { errors: { OrderNumber: ['Order number is already in use.'] } },
    });
    const { page, validation } = createPage({
      create: vi.fn(() => throwError(() => apiError)),
    });
    page.supplierId = 1;
    page.warehouseId = 2;
    page.orderNumber = 'PO-1';
    page.orderDate = '2026-11-18';
    page.currencyCode = 'USD';
    page.exchangeRate = 1;
    page.lines = [{ itemId: 3, description: 'Stock item', quantity: 1, unitCost: 5 }];

    page.saveOrder();

    expect(validation.messagesFor('orderNumber')).toEqual(['Order number is already in use.']);
  });

  it('shows receipt field errors and skips posting invalid receipt details', () => {
    const { page, service, validation } = createPage();
    page.receiveLineId = 5;
    page.receiveQuantity = 2;
    page.receiveDate = '2026-11-18';
    const order = {
      id: 8,
      lines: [{ id: 5, quantity: 3, receivedQuantity: 2 }],
    } as never;

    page.receiveLine(order);

    expect(service.receive).not.toHaveBeenCalled();
    expect(validation.messagesFor('receiveMovementNumber')).toEqual(['Enter a movement number.']);
    expect(validation.messagesFor('receiveQuantity')).toEqual([
      'Quantity cannot exceed the remaining 1 units.',
    ]);
    expect(validation.messagesFor('receiveJournal')).toEqual(['Select a journal.']);
    expect(validation.messagesFor('receivePeriod')).toEqual(['Select an accounting period.']);
    expect(validation.messagesFor('inventoryAccount')).toEqual([
      'Select an inventory asset account.',
    ]);
    expect(validation.messagesFor('offsetAccount')).toEqual(['Select an offset account.']);
  });
});
