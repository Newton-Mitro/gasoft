import { JournalEntryLinesEditorComponent } from './journal-entry-lines-editor.component';

describe('JournalEntryLinesEditorComponent', () => {
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
});
