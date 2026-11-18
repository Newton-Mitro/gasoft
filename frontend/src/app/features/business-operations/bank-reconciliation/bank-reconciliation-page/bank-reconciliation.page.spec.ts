import { ChangeDetectorRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthService } from '../../../../core/auth.service';
import { CompanyService } from '../../../settings/company/company.service';
import { Company } from '../../../settings/company/company.model';
import { FormValidationService } from '../../../../shared/services/form-validation.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { BankReconciliationService } from '../bank-reconciliation.service';
import { BankReconciliationPageComponent } from './bank-reconciliation.page';

describe('BankReconciliationPageComponent validation', () => {
  function createPage(serviceOverrides: Partial<BankReconciliationService> = {}) {
    TestBed.configureTestingModule({
      providers: [
        FormValidationService,
        { provide: NotificationService, useValue: { success: vi.fn(), error: vi.fn() } },
      ],
    });
    const service = {
      createAccount: vi.fn(() => of({})),
      allocate: vi.fn(() => of(undefined)),
      createAdjustment: vi.fn(() => of(undefined)),
      ...serviceOverrides,
    } as unknown as BankReconciliationService;
    const page = TestBed.runInInjectionContext(
      () =>
        new BankReconciliationPageComponent(
          service,
          {} as CompanyService,
          {} as AuthService,
          {} as never,
          {} as never,
          { markForCheck: vi.fn() } as unknown as ChangeDetectorRef,
        ),
    );
    return { page, service, validation: TestBed.inject(FormValidationService) };
  }

  it('shows field errors and skips creating an incomplete bank account', () => {
    const { page, service, validation } = createPage();
    page.company = { functionalCurrencyCode: 'USD' } as Company;

    page.createAccount();

    expect(service.createAccount).not.toHaveBeenCalled();
    expect(validation.messagesFor('name')).toEqual(['Enter an account name.']);
    expect(validation.messagesFor('bankName')).toEqual(['Enter a bank name.']);
    expect(validation.messagesFor('lastFour')).toEqual(['Enter exactly four letters or digits.']);
    expect(validation.messagesFor('chartAccount')).toEqual(['Select a ledger asset account.']);
  });

  it('shows an inline allocation error when the amount exceeds available balances', () => {
    const { page, service, validation } = createPage();
    page.selectedAccountId = 4;
    page.allocationAmounts[31] = 51;

    page.matchTransaction(
      { id: 8, remainingAmount: 50 } as never,
      { journalLineId: 31, amount: 75 } as never,
    );

    expect(service.allocate).not.toHaveBeenCalled();
    expect(validation.messagesFor('matchAmount-31')).toEqual([
      'Allocation cannot exceed the remaining statement or ledger amount.',
    ]);
  });

  it('shows inline errors for missing adjustment account and invalid posting date', () => {
    const { page, service, validation } = createPage();
    page.selectedAccountId = 4;
    page.adjustmentAccountId = null;
    page.adjustmentEntryDate = 'not-a-date';

    page.recordAdjustment({ id: 8 } as never);

    expect(service.createAdjustment).not.toHaveBeenCalled();
    expect(validation.messagesFor('offsetAccount')).toEqual(['Select an offset account.']);
    expect(validation.messagesFor('adjustmentDate')).toEqual(['Enter a valid posting date.']);
  });
});
