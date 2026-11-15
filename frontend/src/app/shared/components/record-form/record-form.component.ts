import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { isBalancedJournalEntry } from '../../../features/general-ledger/journal-entries/journal-entry-lines';
import { Choice, DocumentLineForm, Field, JournalEntryLineForm, RecordFormMode } from '../../models/shared.models';
import { OptionDataService } from '../../data/option-data.service';
import { DocumentLinesEditorComponent } from '../document-lines-editor/document-lines-editor.component';
import { JournalEntryLinesEditorComponent } from '../journal-entry-lines-editor/journal-entry-lines-editor.component';

@Component({
  selector: 'app-record-form',
  imports: [FormsModule, DocumentLinesEditorComponent, JournalEntryLinesEditorComponent],
  styleUrl: './record-form.component.css',
  templateUrl: './record-form.component.html',
})
export class RecordFormComponent {
  constructor(private readonly optionData: OptionDataService) {}

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
      'tax-jurisdictions': 'tax jurisdiction',
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
    if (this.pageKey === 'tax-jurisdictions') return 'Add jurisdiction';
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
}
