import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { isBalancedJournalEntry } from '../../../features/general-ledger/journal-entries/journal-entry-lines';
import { Choice, DocumentLineForm, Field, JournalEntryLineForm, RecordFormMode } from '../../models/shared.models';
import { OptionDataService } from '../../data/option-data.service';
import { DocumentLinesEditorComponent } from '../document-lines-editor/document-lines-editor.component';
import { JournalEntryLinesEditorComponent } from '../journal-entry-lines-editor/journal-entry-lines-editor.component';
import { FormValidationService } from '../../services/form-validation.service';
import { FieldValidationDirective } from '../../directives/field-validation.directive';

@Component({
  selector: 'app-record-form',
  imports: [FormsModule, DocumentLinesEditorComponent, JournalEntryLinesEditorComponent, FieldValidationDirective],
  styleUrl: './record-form.component.css',
  templateUrl: './record-form.component.html',
})
export class RecordFormComponent {
  constructor(
    private readonly optionData: OptionDataService,
    readonly formValidation: FormValidationService,
  ) {}

  @Input() pageKey = '';
  @Input() title = '';
  @Input() formMode: RecordFormMode = 'create';
  @Input() fields: Field[] = [];
  @Input() form: Record<string, unknown> = {};
  @Input() options: Record<string, Choice[]> = {};
  @Input() optionsLoading: string[] = [];
  @Input() optionsFailed: string[] = [];
  @Input() documentLines: DocumentLineForm[] = [];
  @Input() journalEntryLines: JournalEntryLineForm[] = [];
  @Input() error = '';
  @Input() saving = false;

  @Output() readonly valueChange = new EventEmitter<{ key: string; value: unknown }>();
  @Output() readonly documentLinesChange = new EventEmitter<DocumentLineForm[]>();
  @Output() readonly journalEntryLinesChange = new EventEmitter<JournalEntryLineForm[]>();
  @Output() readonly submitForm = new EventEmitter<void>();
  @Output() readonly cancel = new EventEmitter<void>();

  get journalEntryBalanced(): boolean {
    return isBalancedJournalEntry(this.journalEntryLines);
  }

  get heading(): string {
    const names: Record<string, string> = {
      accounts: 'account',
      'account-groups': 'account group',
      'fiscal-years': 'fiscal year',
      periods: 'accounting period',
      journals: 'journal',
      'journal-entries': 'journal entry',
      parties: 'customer or supplier',
      'party-addresses': 'address',
      'sales-invoices': 'sales invoice',
      payments: 'payment',
      items: 'inventory item',
      'item-categories': 'item category',
      warehouses: 'warehouse',
      'inventory-counts': 'inventory count',
      'tax-registrations': 'tax registration',

      companies: 'organization',
    };
    if (this.formMode === 'post') return 'Posting details';
    if (this.formMode === 'allocate') return 'Allocate payment';
    if (this.formMode === 'receive') return 'Record stock receipt';
    if (this.formMode === 'issue') return 'Record stock issue';
    if (this.formMode === 'location') return 'Add warehouse location';
    const name = names[this.pageKey] ?? this.title.toLowerCase().replace(/s$/, '');
    return `${this.formMode === 'edit' ? 'Edit' : 'Add'} ${name}`;
  }

  get eyebrow(): string {
    if (this.formMode === 'edit') return 'EDIT RECORD';
    if (this.formMode === 'post' || this.formMode === 'countPost') return 'POST TO LEDGER';
    if (this.formMode === 'allocate') return 'PAYMENT ALLOCATION';
    if (this.formMode === 'receive') return 'STOCK RECEIPT';
    if (this.formMode === 'issue') return 'STOCK ISSUE';
    return 'NEW ENTRY';
  }

  get modeLabel(): string {
    if (this.formMode === 'edit') return 'EDITING';
    if (this.formMode === 'post' || this.formMode === 'countPost') return 'POSTING';
    if (this.formMode === 'allocate') return 'ALLOCATING';
    if (this.formMode === 'receive') return 'RECEIVING';
    if (this.formMode === 'issue') return 'ISSUING';
    if (this.formMode === 'location') return 'NEW LOCATION';
    return 'NEW';
  }

  get submitLabel(): string {
    if (this.formMode === 'post' || this.formMode === 'countPost') return 'Post to ledger';
    if (this.formMode === 'allocate') return 'Save allocation';
    if (this.formMode === 'receive') return 'Record receipt';
    if (this.formMode === 'issue') return 'Record issue';
    if (this.formMode === 'location') return 'Create location';
    if (this.formMode === 'edit') return 'Save changes';
    if (this.pageKey === 'sales-invoices') return 'Create invoice';
    if (this.pageKey === 'purchase-bills') return 'Create bill';
    if (this.pageKey === 'journal-entries') return 'Save draft';
    if (this.pageKey === 'inventory-counts') return 'Create count';
    if (this.pageKey === 'companies') return 'Create organization';
    return 'Create record';
  }

  get actionHint(): string {
    if (this.formMode === 'post' || this.formMode === 'countPost') return 'Posting will create a ledger entry.';
    if (this.formMode === 'allocate') return 'Review the allocation details before saving.';
    return 'You can review this record after saving.';
  }

  isOptionLoading(source: string | undefined): boolean {
    return !!source && (
      this.optionsLoading.includes(source) || this.optionData.status(source)() === 'loading'
    );
  }

  hasOptionFailed(source: string | undefined): boolean {
    return !!source && (
      this.optionsFailed.includes(source) || this.optionData.status(source)() === 'failed'
    );
  }

  hasPendingOptions(): boolean {
    return this.optionsLoading.length > 0 ||
      this.fields.some(field => this.isOptionLoading(field.optionSource));
  }

  hasFailedOptions(): boolean {
    return this.optionsFailed.length > 0 ||
      this.fields.some(field => this.hasOptionFailed(field.optionSource));
  }

  fieldErrors(key: string): string[] {
    return this.formValidation.messagesFor(key);
  }

  clearFieldError(key: string): void {
    this.formValidation.clearField(key);
  }

  submit(): void {
    const errors = Object.fromEntries(
      this.fields.flatMap(field => {
        const value = this.form[field.key];
        const label = field.label.toLowerCase();
        const empty = value === null || value === undefined ||
          typeof value === 'string' && value.trim() === '';
        if (field.required && empty) {
          const message = field.type === 'select'
            ? `Select ${label}.`
            : field.type === 'date'
              ? `Enter a valid ${label} date.`
              : `Enter ${label}.`;
          return [[field.key, [message]]];
        }

        if (empty) return [];
        if (field.key.endsWith('Id') && !isPositiveInteger(value)) {
          return [[field.key, [`Select a valid ${label}.`]]];
        }
        if (field.type === 'number') {
          const number = Number(value);
          if (!Number.isFinite(number)) return [[field.key, [`Enter a valid ${label}.`]]];
          if (field.min !== undefined && number < field.min) {
            return [[field.key, [`${field.label} must be at least ${field.min}.`]]];
          }
          if (field.max !== undefined && number > field.max) {
            return [[field.key, [`${field.label} must be no more than ${field.max}.`]]];
          }
        }
        if (typeof value === 'string') {
          if (field.minLength !== undefined && value.length < field.minLength) {
            return [[field.key, [`${field.label} must be at least ${field.minLength} characters.`]]];
          }
          if (field.maxLength !== undefined && value.length > field.maxLength) {
            return [[field.key, [`${field.label} must be no more than ${field.maxLength} characters.`]]];
          }
          if (field.pattern && !new RegExp(field.pattern).test(value)) {
            return [[field.key, [`Enter a valid ${label}.`]]];
          }
        }
        return [];
      }),
    );
    if (Object.keys(errors).length) {
      this.formValidation.setErrors(errors);
      return;
    }

    this.formValidation.clear();
    this.submitForm.emit();
  }
}

function isPositiveInteger(value: unknown): boolean {
  if (typeof value !== 'number' && typeof value !== 'string') return false;
  const numeric = Number(value);
  return Number.isSafeInteger(numeric) && numeric > 0;
}
