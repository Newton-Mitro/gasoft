import { Component, EventEmitter, Input, Output } from '@angular/core';
import {
  isBalancedJournalEntry,
  isValidJournalEntryLine,
  JOURNAL_ENTRY_BALANCE_TOLERANCE,
} from '../../../features/general-ledger/journal-entries/journal-entry-lines';
import { Choice, JournalEntryLineForm } from '../../models/shared.models';

@Component({
  selector: 'app-journal-entry-lines-editor',
  styleUrl: './journal-entry-lines-editor.component.css',
  templateUrl: './journal-entry-lines-editor.component.html',
})
export class JournalEntryLinesEditorComponent {
  @Input() lines: JournalEntryLineForm[] = [];
  @Input() accounts: Choice[] = [];
  @Input() currencyCode = '';
  @Output() readonly linesChange = new EventEmitter<JournalEntryLineForm[]>();

  get totalDebits(): number {
    return this.lines.reduce((sum, line) => sum + (line.debit ?? 0), 0);
  }

  get totalCredits(): number {
    return this.lines.reduce((sum, line) => sum + (line.credit ?? 0), 0);
  }

  get difference(): number {
    return this.totalDebits - this.totalCredits;
  }

  get hasBalancedLines(): boolean {
    return isBalancedJournalEntry(this.lines);
  }

  get balanceMessage(): string {
    if (this.lines.length < 2) return 'Add at least two lines to create a journal entry.';
    if (this.lines.some(line => line.accountId === null)) return 'Choose an account for every line.';
    if (this.lines.some(line => !isValidJournalEntryLine(line))) return 'Choose an account and enter a positive debit or credit on each line, but not both.';
    if (Math.abs(this.difference) > JOURNAL_ENTRY_BALANCE_TOLERANCE) {
      return `Entry is out of balance by ${this.formatAmount(Math.abs(this.difference))}.`;
    }
    return 'Debits and credits are balanced.';
  }

  addLine(): void {
    if (this.lines.length >= 1000) return;
    this.emitLines([...this.lines, this.newLine()]);
  }

  removeLine(index: number): void {
    if (this.lines.length <= 2) return;
    this.emitLines(this.lines.filter((_, lineIndex) => lineIndex !== index));
  }

  updateLine(index: number, key: keyof JournalEntryLineForm, value: unknown): void {
    if (!this.lines[index]) return;
    const updated = this.lines.map(line => ({ ...line }));
    const line = updated[index];
    if (key === 'accountId') {
      line.accountId = value === '' || value === null ? null : Number(value);
    } else {
      const amount = value === '' || value === null ? null : Number(value);
      line[key] = amount;
      if (amount && key === 'debit') line.credit = 0;
      if (amount && key === 'credit') line.debit = 0;
    }
    this.emitLines(updated);
  }

  formatAmount(value: number): string {
    if (!Number.isFinite(value)) return '—';
    if (!this.currencyCode) return value.toFixed(2);
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: this.currencyCode,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value);
  }

  private emitLines(lines: JournalEntryLineForm[]): void {
    this.linesChange.emit(lines);
  }

  private newLine(): JournalEntryLineForm {
    return { accountId: null, debit: null, credit: null };
  }
}
