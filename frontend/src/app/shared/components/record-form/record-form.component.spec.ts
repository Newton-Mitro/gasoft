import { RecordFormComponent } from './record-form.component';

describe('RecordFormComponent option loading', () => {
  it('identifies option sources that are loading or failed', () => {
    const component = new RecordFormComponent();
    component.optionsLoading = ['journals'];
    component.optionsFailed = ['accounts'];

    expect(component.isOptionLoading('journals')).toBe(true);
    expect(component.isOptionLoading('accounts')).toBe(false);
    expect(component.hasOptionFailed('accounts')).toBe(true);
    expect(component.hasOptionFailed(undefined)).toBe(false);
  });
});
