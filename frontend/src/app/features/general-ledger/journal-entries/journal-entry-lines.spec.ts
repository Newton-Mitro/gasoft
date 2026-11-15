import { isBalancedJournalEntry } from './journal-entry-lines';

describe('isBalancedJournalEntry', () => {
  it('accepts complete, balanced entries with multiple pairs', () => {
    expect(isBalancedJournalEntry([
      { accountId: 11, debit: 100, credit: 0 },
      { accountId: 12, debit: 0, credit: 100 },
      { accountId: 13, debit: 25, credit: 0 },
      { accountId: 14, debit: 0, credit: 25 },
    ])).toBe(true);
  });

  it('rejects unbalanced entries and lines without an account', () => {
    expect(isBalancedJournalEntry([
      { accountId: 11, debit: 100, credit: 0 },
      { accountId: 12, debit: 0, credit: 99 },
    ])).toBe(false);
    expect(isBalancedJournalEntry([
      { accountId: null, debit: 100, credit: 0 },
      { accountId: 12, debit: 0, credit: 100 },
    ])).toBe(false);
  });
});
