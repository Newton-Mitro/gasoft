import { Field } from '../../models/shared.models';
import { RecordPreviewComponent, shouldShowRecordPreview } from './record-preview.component';

describe('record preview', () => {
  it('only enables configured create or edit forms with at most eight fields', () => {
    const fields: Field[] = Array.from({ length: 8 }, (_, index) => ({
      key: `field${index}`,
      label: `Field ${index}`,
    }));

    expect(shouldShowRecordPreview('accounts', 'create', fields)).toBe(true);
    expect(shouldShowRecordPreview('accounts', 'edit', fields)).toBe(true);
    expect(shouldShowRecordPreview('accounts', 'receive', fields)).toBe(false);
    expect(shouldShowRecordPreview('accounts', 'create', [...fields, { key: 'extra', label: 'Extra' }])).toBe(false);
    expect(shouldShowRecordPreview('unknown-page', 'create', fields)).toBe(false);
  });

  it('formats a payment total, currency, and selected choices for the preview', () => {
    const component = new RecordPreviewComponent();
    component.pageKey = 'payments';
    component.fields = [
      { key: 'amount', label: 'Amount', type: 'number' },
      { key: 'currencyCode', label: 'Currency code', type: 'select', options: [{ label: 'Bangladeshi taka', value: 'BDT' }] },
      { key: 'direction', label: 'Direction', type: 'select', options: [{ label: 'Incoming', value: 'Receive' }] },
    ];
    component.form = { amount: '1234.5', currencyCode: 'BDT', direction: 'Receive' };

    expect(component.primaryValue).toBe('1,234.50');
    expect(component.primarySuffix).toBe('BDT');
    expect(component.detailFields.map(field => field.key)).toEqual(['direction', 'currencyCode']);
    expect(component.formatFieldValue(component.detailFields[0])).toBe('Incoming');
  });

  it('includes the ledger link in a bank account preview', () => {
    const component = new RecordPreviewComponent();
    component.pageKey = 'bank-accounts';
    component.fields = [
      { key: 'name', label: 'Account name' },
      { key: 'bankName', label: 'Bank name' },
      { key: 'lastFour', label: 'Last four digits' },
      { key: 'chartOfAccountId', label: 'Ledger asset account', type: 'select', optionSource: 'accounts' },
      { key: 'currencyCode', label: 'Functional currency' },
    ];

    expect(component.configuration.detailFields).toContain('chartOfAccountId');
    expect(component.fields.find(field => field.key === 'chartOfAccountId')).toBeDefined();
    expect(component.detailFields.map(field => field.key)).toEqual([
      'bankName',
      'lastFour',
      'chartOfAccountId',
      'currencyCode',
    ]);
  });

  it('does not expose a temporary password in the team member preview', () => {
    const component = new RecordPreviewComponent();
    component.pageKey = 'users';
    component.fields = [
      { key: 'displayName', label: 'Full name' },
      { key: 'email', label: 'Email' },
      { key: 'role', label: 'Role', type: 'select', options: [{ label: 'Admin', value: 'Admin' }] },
      { key: 'temporaryPassword', label: 'Temporary password', type: 'password' },
    ];
    component.form = { displayName: 'Alex Morgan', email: 'alex@example.com', role: 'Admin', temporaryPassword: 'not-for-preview' };

    expect(component.primaryValue).toBe('Alex Morgan');
    expect(component.detailFields.map(field => field.key)).toEqual(['email', 'role']);
    expect(component.detailFields.map(field => component.formatFieldValue(field))).toEqual(['alex@example.com', 'Admin']);
  });
});
