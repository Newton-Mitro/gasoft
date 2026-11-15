import { DocumentLinesEditorComponent } from './document-lines-editor.component';

describe('DocumentLinesEditorComponent', () => {
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
});
