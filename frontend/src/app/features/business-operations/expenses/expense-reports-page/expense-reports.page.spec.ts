import { TestBed } from '@angular/core/testing';
import { FormValidationService } from '../../../../shared/services/form-validation.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { AuthService } from '../../../../core/auth.service';
import { ExpenseReportsPageComponent } from './expense-reports.page';

describe('ExpenseReportsPageComponent draft validation', () => {
  let validation: FormValidationService;
  let create: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    create = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { companyId: () => 1 } },
        {
          provide: NotificationService,
          useValue: { success: vi.fn(), error: vi.fn() },
        },
      ],
    });
    validation = TestBed.inject(FormValidationService);
  });

  it('shows field-level errors and prevents saving an incomplete expense report', async () => {
    const page = TestBed.runInInjectionContext(
      () =>
        new ExpenseReportsPageComponent(
          {} as never,
          {} as never,
          {} as never,
          {} as never,
          { create } as never,
          { markForCheck: vi.fn() } as never,
        ),
    );
    page.expenseLines = [
      {
        id: null,
        expenseDate: 'invalid-date',
        merchant: '',
        description: '',
        accountId: null,
        amount: null,
        receiptFile: null,
        receiptUrl: null,
      },
    ];

    await page.saveReport();

    expect(create).not.toHaveBeenCalled();
    expect(validation.messagesFor('reportTitle')).toEqual(['Enter a report title.']);
    expect(validation.messagesFor('lines[0].expenseDate')).toEqual([
      'Enter a valid expense date.',
    ]);
    expect(validation.messagesFor('lines[0].accountId')).toEqual([
      'Select an expense account.',
    ]);
    expect(validation.messagesFor('lines[0].amount')).toEqual([
      'Amount must be greater than zero.',
    ]);
  });
});
