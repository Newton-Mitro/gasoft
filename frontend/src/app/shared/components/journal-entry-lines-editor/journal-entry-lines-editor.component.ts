import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  isBalancedJournalEntry,
  isValidJournalEntryLine,
  JOURNAL_ENTRY_BALANCE_TOLERANCE,
} from '../../../features/general-ledger/journal-entries/journal-entry-lines';
import { Choice, JournalEntryLineForm } from '../../models/shared.models';
import { FieldValidationDirective } from '../../directives/field-validation.directive';
import { SearchableSelectDirective } from '../../directives/searchable-select.directive';
import { InputComponent } from '../input/input.component';

@Component({
  selector: 'app-journal-entry-lines-editor',
  imports: [FormsModule, FieldValidationDirective, SearchableSelectDirective, InputComponent],
  styleUrl: './journal-entry-lines-editor.component.css',
  templateUrl: './journal-entry-lines-editor.component.html',
})
export class JournalEntryLinesEditorComponent {
  @Input() lines: JournalEntryLineForm[] = [];
  @Input() accounts: Choice[] = [];
  @Input() accountsLoading = false;
  @Input() accountsFailed = false;
  @Input() currencyCode = '';
  @Input() balancingAccountLabel: string | null = null;
  @Input() maxLines = 1000;
  @Output() readonly linesChange = new EventEmitter<JournalEntryLineForm[]>();

  get minimumLines(): number {
    return this.balancingAccountLabel ? 1 : 2;
  }

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
    if (this.balancingAccountLabel) {
      return this.lines.length >= this.minimumLines &&
        this.lines.every(isValidJournalEntryLine);
    }
    return isBalancedJournalEntry(this.lines);
  }

  get balanceMessage(): string {
    if (this.lines.length < this.minimumLines) {
      return this.balancingAccountLabel
        ? 'Add at least one starting-balance line.'
        : 'Add at least two lines to create a journal entry.';
    }
    if (this.lines.some(line => line.accountId === null)) return 'Choose an account for every line.';
    if (this.lines.some(line => !isValidJournalEntryLine(line))) return 'Choose an account and enter a positive debit or credit on each line, but not both.';
    if (Math.abs(this.difference) > JOURNAL_ENTRY_BALANCE_TOLERANCE) {
      if (this.balancingAccountLabel) {
        return `${this.balancingAccountLabel} will be ${this.difference > 0 ? 'credited' : 'debited'} by ${this.formatAmount(Math.abs(this.difference))}.`;
      }
      return `Entry is out of balance by ${this.formatAmount(Math.abs(this.difference))}.`;
    }
    if (this.balancingAccountLabel) return 'Starting balances are balanced; no adjustment is needed.';
    return 'Debits and credits are balanced.';
  }

  addLine(): void {
    if (this.lines.length >= this.maxLines) return;
    this.emitLines([...this.lines, this.newLine()]);
  }

  removeLine(index: number): void {
    if (this.lines.length <= this.minimumLines) return;
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
