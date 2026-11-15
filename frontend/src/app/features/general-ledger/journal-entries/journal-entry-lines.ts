import { JournalEntryLineForm } from '../../../shared/models/shared.models';

const MAX_AMOUNT = 99_999_999_999_999.999999;
export const JOURNAL_ENTRY_BALANCE_TOLERANCE = 0.000001;

export function isBalancedJournalEntry(lines: JournalEntryLineForm[]): boolean {
  if (lines.length < 2) return false;

  let debits = 0;
  let credits = 0;
  for (const line of lines) {
    if (!isValidJournalEntryLine(line)) return false;
    const debit = line.debit ?? 0;
    const credit = line.credit ?? 0;
    debits += debit;
    credits += credit;
  }

  return Math.abs(debits - credits) <= JOURNAL_ENTRY_BALANCE_TOLERANCE;
}

export function isValidJournalEntryLine(line: JournalEntryLineForm): boolean {
  const debit = line.debit ?? 0;
  const credit = line.credit ?? 0;
  return line.accountId !== null &&
    Number.isFinite(debit) && debit >= 0 && debit <= MAX_AMOUNT &&
    Number.isFinite(credit) && credit >= 0 && credit <= MAX_AMOUNT &&
    (debit > 0) !== (credit > 0);
}
