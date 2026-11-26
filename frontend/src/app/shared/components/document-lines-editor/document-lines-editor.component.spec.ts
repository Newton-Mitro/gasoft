import { TestBed } from '@angular/core/testing';
import { DocumentLinesEditorComponent } from './document-lines-editor.component';

describe('DocumentLinesEditorComponent', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('adds and removes lines while keeping at least one line', () => {
    const component = new DocumentLinesEditorComponent();
    component.lines = [{ accountId: null, taxCodeId: null, description: '', quantity: 1, unitPrice: 0 }];
    let updatedLines = component.lines;
    component.linesChange.subscribe(lines => updatedLines = lines);

    component.addLine();
    expect(updatedLines).toHaveLength(2);
    component.lines = updatedLines;

    component.removeLine(1);
    expect(updatedLines).toHaveLength(1);
    component.lines = updatedLines;
    component.removeLine(0);
    expect(updatedLines).toHaveLength(1);
  });

  it('updates line values immutably and calculates the subtotal', () => {
    const component = new DocumentLinesEditorComponent();
    const original = { accountId: 7, taxCodeId: null, description: 'Service', quantity: 2, unitPrice: 12.5 };
    component.lines = [original];
    let updatedLines = component.lines;
    component.linesChange.subscribe(lines => updatedLines = lines);

    component.updateLine(0, 'quantity', '3');

    expect(original.quantity).toBe(2);
    expect(updatedLines[0].quantity).toBe(3);
    component.lines = updatedLines;
    expect(component.subtotal).toBe(37.5);
  });

  it('keeps account and tax selections when options arrive after the document', async () => {
    TestBed.configureTestingModule({ imports: [DocumentLinesEditorComponent] });
    const fixture = TestBed.createComponent(DocumentLinesEditorComponent);
    fixture.componentInstance.lines = [
      { accountId: 10, taxCodeId: 20, description: 'Item', quantity: 1, unitPrice: 100 },
    ];
    fixture.detectChanges();

    fixture.componentInstance.accounts = [
      { value: 10, label: '4000 · Sales' },
      { value: 11, label: '5000 · Expense' },
    ];
    fixture.componentInstance.taxCodes = [
      { value: 20, label: 'VAT · Sales tax' },
      { value: 21, label: 'GST · Goods and services tax' },
    ];
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const selects = Array.from(fixture.nativeElement.querySelectorAll('select')) as HTMLSelectElement[];
    expect(selects.map(select => select.selectedOptions[0]?.textContent?.trim())).toEqual([
      '4000 · Sales',
      'VAT · Sales tax',
    ]);

    fixture.destroy();
  });
});
