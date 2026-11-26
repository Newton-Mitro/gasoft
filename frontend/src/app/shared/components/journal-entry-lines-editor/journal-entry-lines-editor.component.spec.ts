import { TestBed } from '@angular/core/testing';
import { JournalEntryLinesEditorComponent } from './journal-entry-lines-editor.component';

describe('JournalEntryLinesEditorComponent', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('adds and removes lines while preserving the minimum of two', () => {
    const component = new JournalEntryLinesEditorComponent();
    component.lines = [
      { accountId: null, debit: null, credit: null },
      { accountId: null, debit: null, credit: null },
    ];
    let updatedLines = component.lines;
    component.linesChange.subscribe(lines => updatedLines = lines);

    component.addLine();
    expect(updatedLines).toHaveLength(3);
    component.lines = updatedLines;

    component.removeLine(2);
    expect(updatedLines).toHaveLength(2);
    component.lines = updatedLines;
    component.removeLine(0);
    expect(updatedLines).toHaveLength(2);
  });

  it('sets the opposite side to zero and reports balanced totals', () => {
    const component = new JournalEntryLinesEditorComponent();
    component.lines = [
      { accountId: 11, debit: null, credit: null },
      { accountId: 12, debit: null, credit: null },
    ];
    let updatedLines = component.lines;
    component.linesChange.subscribe(lines => updatedLines = lines);

    component.updateLine(0, 'debit', '25');
    component.lines = updatedLines;
    component.updateLine(1, 'credit', '25');
    component.lines = updatedLines;

    expect(updatedLines).toEqual([
      { accountId: 11, debit: 25, credit: 0 },
      { accountId: 12, debit: 0, credit: 25 },
    ]);
    expect(component.totalDebits).toBe(25);
    expect(component.totalCredits).toBe(25);
    expect(component.hasBalancedLines).toBe(true);
    expect(component.balanceMessage).toBe('Debits and credits are balanced.');
  });

  it('accepts a starting-balance line and explains the Opening Balance Equity adjustment', () => {
    const component = new JournalEntryLinesEditorComponent();
    component.balancingAccountLabel = 'Opening Balance Equity';
    component.lines = [{ accountId: 10, debit: 500, credit: 0 }];

    expect(component.hasBalancedLines).toBe(true);
    expect(component.balanceMessage).toBe('Opening Balance Equity will be credited by 500.00.');
  });

  it('keeps the normal two-line balance requirement for journal entries', () => {
    const component = new JournalEntryLinesEditorComponent();
    component.lines = [{ accountId: 10, debit: 500, credit: 0 }];

    expect(component.hasBalancedLines).toBe(false);
    expect(component.balanceMessage).toBe('Add at least two lines to create a journal entry.');
  });

  it('keeps line accounts selected when account options arrive after the entry', async () => {
    TestBed.configureTestingModule({ imports: [JournalEntryLinesEditorComponent] });
    const fixture = TestBed.createComponent(JournalEntryLinesEditorComponent);
    fixture.componentInstance.lines = [
      { accountId: 10, debit: 100, credit: 0 },
      { accountId: 11, debit: 0, credit: 100 },
    ];
    fixture.detectChanges();

    fixture.componentInstance.accounts = [
      { value: 10, label: '1000 · Cash and cash equivalents' },
      { value: 11, label: '1100 · Accounts receivable' },
    ];
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const selects = Array.from(fixture.nativeElement.querySelectorAll('select')) as HTMLSelectElement[];
    expect(selects.map(select => select.selectedOptions[0]?.textContent?.trim())).toEqual([
      '1000 · Cash and cash equivalents',
      '1100 · Accounts receivable',
    ]);
    const triggers = Array.from(fixture.nativeElement.querySelectorAll('.searchable-select-trigger')) as HTMLButtonElement[];
    expect(triggers.map(trigger => trigger.textContent?.trim())).toEqual([
      '1000 · Cash and cash equivalents',
      '1100 · Accounts receivable',
    ]);

    fixture.destroy();
  });
});
