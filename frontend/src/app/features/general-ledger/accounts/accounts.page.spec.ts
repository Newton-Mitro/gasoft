import { ChangeDetectorRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { NotificationService } from '../../../shared/services/notification.service';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { AccountService } from './account.service';
import { Account } from './account.model';
import { AccountsPageComponent } from './accounts.page';

const accounts: Account[] = [
  { id: 1, companyId: 1, accountGroupId: 10, parentId: null, code: '1000', name: 'Cash', allowPosting: false, isActive: true },
  { id: 2, companyId: 1, accountGroupId: 10, parentId: 1, code: '1010', name: 'Bank', allowPosting: false, isActive: true },
  { id: 3, companyId: 1, accountGroupId: 10, parentId: 2, code: '1011', name: 'Checking', allowPosting: true, isActive: true },
];

describe('AccountsPageComponent drag and drop', () => {
  function createPage(): AccountsPageComponent {
    TestBed.configureTestingModule({
      providers: [
        { provide: NotificationService, useValue: { success: vi.fn(), error: vi.fn() } },
      ],
    });
    const accountsService = { update: vi.fn() } as unknown as AccountService;
    return TestBed.runInInjectionContext(() => new AccountsPageComponent(
      {} as ActivatedRoute,
      {} as Router,
      accountsService,
      {} as OptionDataService,
      { markForCheck: vi.fn() } as unknown as ChangeDetectorRef,
    ));
  }

  it('prevents an invalid account drop from bubbling into the group root drop zone', () => {
    const page = createPage();
    page.rows = accounts;
    page.draggedAccountId = 1;
    const event = {
      preventDefault: vi.fn(),
      stopPropagation: vi.fn(),
      dataTransfer: { dropEffect: 'move' },
    } as unknown as DragEvent;

    page.onAccountDragOver(event, accounts[2]!);

    expect(event.stopPropagation).toHaveBeenCalled();
    expect(event.preventDefault).not.toHaveBeenCalled();
    expect(page.dropBlockedAccountId).toBe(3);
    expect(page.dropTargetGroupId).toBeNull();
  });

  it('accepts a valid account target and prevents the group root from handling it', () => {
    const page = createPage();
    page.rows = accounts;
    page.draggedAccountId = 3;
    const event = {
      preventDefault: vi.fn(),
      stopPropagation: vi.fn(),
      dataTransfer: { dropEffect: 'none' },
    } as unknown as DragEvent;

    page.onAccountDragOver(event, accounts[0]!);

    expect(event.preventDefault).toHaveBeenCalled();
    expect(event.stopPropagation).toHaveBeenCalled();
    expect(event.dataTransfer?.dropEffect).toBe('move');
    expect(page.dropTargetAccountId).toBe(1);
  });
});
