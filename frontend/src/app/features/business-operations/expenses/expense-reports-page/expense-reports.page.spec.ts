import { TestBed } from '@angular/core/testing';
import { FormValidationService } from '../../../../shared/services/form-validation.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { AuthService } from '../../../../core/auth.service';
import { ExpenseReportsPageComponent } from './expense-reports.page';

describe('ExpenseReportsPageComponent', () => {
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

  it('creates receipt previews and revokes replaced or discarded object URLs', () => {
    const page = TestBed.runInInjectionContext(
      () =>
        new ExpenseReportsPageComponent(
          {} as never,
          {} as never,
          {} as never,
          {} as never,
          {} as never,
          { markForCheck: vi.fn() } as never,
        ),
    );
    const createObjectUrl = vi
      .spyOn(URL, 'createObjectURL')
      .mockReturnValueOnce('blob:first-receipt')
      .mockReturnValueOnce('blob:second-receipt');
    const revokeObjectUrl = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    page.addLine();

    const selectReceipt = (file: File) => {
      const input = document.createElement('input');
      Object.defineProperty(input, 'files', { value: [file] });
      const event = new Event('change');
      Object.defineProperty(event, 'target', { value: input });
      page.selectReceipt(0, event);
    };

    selectReceipt(new File(['image'], 'first.png', { type: 'image/png' }));
    expect(page.expenseLines[0].receiptPreviewUrl).toBeTruthy();
    expect(page.expenseLines[0].receiptObjectUrl).toBe('blob:first-receipt');

    selectReceipt(new File(['pdf'], 'second.pdf', { type: 'application/pdf' }));
    expect(revokeObjectUrl).toHaveBeenCalledWith('blob:first-receipt');
    expect(page.expenseLines[0].receiptObjectUrl).toBe('blob:second-receipt');

    page.ngOnDestroy();
    expect(revokeObjectUrl).toHaveBeenCalledWith('blob:second-receipt');
    expect(createObjectUrl).toHaveBeenCalledTimes(2);
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
        receiptObjectUrl: null,
        receiptPreviewUrl: null,
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
