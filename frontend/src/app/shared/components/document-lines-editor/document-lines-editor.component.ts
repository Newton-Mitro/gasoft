import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Choice, DocumentLineForm } from '../../models/shared.models';
import { FieldValidationDirective } from '../../directives/field-validation.directive';
import { SearchableSelectDirective } from '../../directives/searchable-select.directive';
import { InputComponent } from '../input/input.component';

@Component({
  selector: 'app-document-lines-editor',
  imports: [FieldValidationDirective, SearchableSelectDirective, InputComponent],
  styleUrl: './document-lines-editor.component.css',
  templateUrl: './document-lines-editor.component.html',
})
export class DocumentLinesEditorComponent {
  @Input() lines: DocumentLineForm[] = [];
  @Input() accounts: Choice[] = [];
  @Input() taxCodes: Choice[] = [];
  @Input() accountsLoading = false;
  @Input() accountsFailed = false;
  @Input() taxCodesLoading = false;
  @Input() taxCodesFailed = false;
  @Input() accountLabel = 'Account';
  @Input() currencyCode = '';
  @Output() readonly linesChange = new EventEmitter<DocumentLineForm[]>();

  addLine(): void {
    if (this.lines.length >= 1000) return;
    this.linesChange.emit([...this.lines, this.newLine()]);
  }

  removeLine(index: number): void {
    if (this.lines.length <= 1) return;
    this.linesChange.emit(this.lines.filter((_, lineIndex) => lineIndex !== index));
  }

  updateLine(index: number, key: keyof DocumentLineForm, value: unknown): void {
    const line = this.lines[index];
    if (!line) return;
    const updated = this.lines.map(item => ({ ...item }));
    const target = updated[index];
    if (key === 'accountId' || key === 'taxCodeId') {
      target[key] = value === null || value === '' ? null : Number(value);
    } else if (key === 'quantity' || key === 'unitPrice') {
      target[key] = value === null || value === '' ? 0 : Number(value);
    } else {
      target[key] = String(value ?? '');
    }
    this.linesChange.emit(updated);
  }

  get subtotal(): number {
    return this.lines.reduce((total, line) => total + line.quantity * line.unitPrice, 0);
  }

  get formattedSubtotal(): string {
    if (!this.currencyCode) return String(this.subtotal);
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: this.currencyCode,
      maximumFractionDigits: 2,
    }).format(this.subtotal);
  }

  private newLine(): DocumentLineForm {
    return { accountId: null, taxCodeId: null, description: '', quantity: 1, unitPrice: 0 };
  }
}
