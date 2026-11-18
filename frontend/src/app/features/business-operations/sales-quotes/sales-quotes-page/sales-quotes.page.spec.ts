import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ApiService } from '../../../../core/api.service';
import { OptionDataService } from '../../../../shared/data/option-data.service';
import { FormValidationService } from '../../../../shared/services/form-validation.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { SalesQuotesService } from '../sales-quotes.service';
import { SalesQuotesPageComponent } from './sales-quotes.page';

describe('SalesQuotesPageComponent validation', () => {
  function createPage(serviceOverrides: Partial<SalesQuotesService> = {}) {
    TestBed.configureTestingModule({
      providers: [
        FormValidationService,
        { provide: NotificationService, useValue: { success: vi.fn(), error: vi.fn() } },
      ],
    });
    const service = {
      create: vi.fn(() => of({})),
      update: vi.fn(() => of({})),
      convert: vi.fn(() => of({})),
      ...serviceOverrides,
    } as unknown as SalesQuotesService;
    const page = TestBed.runInInjectionContext(() => new SalesQuotesPageComponent(
      service,
      {} as OptionDataService,
      {} as ApiService,
      { markForCheck: vi.fn() } as unknown as ChangeDetectorRef,
    ));
    return { page, service, validation: TestBed.inject(FormValidationService) };
  }

  it('shows field-level errors and does not save invalid quote details', () => {
    const { page, service, validation } = createPage();
    page.lines = [{
      accountId: null,
      taxCodeId: null,
      description: '',
      quantity: 0,
      unitPrice: -1,
    }];

    page.save();

    expect(service.create).not.toHaveBeenCalled();
    expect(validation.messagesFor('customer')).toEqual(['Select a customer.']);
    expect(validation.messagesFor('quoteNumber')).toEqual(['Enter a quote number.']);
    expect(validation.messagesFor('account-0')).toEqual(['Select a revenue account.']);
    expect(validation.messagesFor('description-0')).toEqual(['Enter a line description.']);
    expect(validation.messagesFor('quantity-0')).toEqual(['Quantity must be greater than zero.']);
    expect(validation.messagesFor('price-0')).toEqual(['Unit price cannot be negative.']);
  });

  it('maps API validation errors back to quote and line fields', () => {
    const apiError = new HttpErrorResponse({
      status: 400,
      error: {
        errors: {
          CustomerId: ['Select a valid customer.'],
          'Lines[0].Quantity': ['Quantity exceeds the limit.'],
        },
      },
    });
    const { page, validation } = createPage({
      create: vi.fn(() => throwError(() => apiError)),
    });
    page.customerId = 1;
    page.quoteNumber = 'Q-1';
    page.quoteDate = '2026-11-18';
    page.validUntil = '2026-12-18';
    page.currencyCode = 'USD';
    page.exchangeRate = 1;
    page.lines = [{
      accountId: 2,
      taxCodeId: null,
      description: 'Service',
      quantity: 1,
      unitPrice: 100,
    }];

    page.save();

    expect(validation.messagesFor('customer')).toEqual(['Select a valid customer.']);
    expect(validation.messagesFor('quantity-0')).toEqual(['Quantity exceeds the limit.']);
  });

  it('requires a valid date order when converting a quote', () => {
    const { page, validation } = createPage();
    page.convertingQuote = {
      id: 1,
      quoteNumber: 'Q-1',
    } as never;
    page.invoiceNumber = 'INV-Q-1';
    page.invoiceDate = '2026-12-18';
    page.dueDate = '2026-11-18';

    page.convert();

    expect(validation.messagesFor('dueDate')).toEqual([
      'Due date must be on or after the invoice date.',
    ]);
  });
});
