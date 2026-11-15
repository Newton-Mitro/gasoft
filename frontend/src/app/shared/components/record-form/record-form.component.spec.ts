import { signal } from '@angular/core';
import { RecordFormComponent } from './record-form.component';

describe('RecordFormComponent option loading', () => {
  it('identifies option sources that are loading or failed', () => {
    const optionData = { status: () => signal<'idle' | 'loading' | 'loaded' | 'failed'>('idle') };
    const component = new RecordFormComponent(optionData as never);
    component.optionsLoading = ['journals'];
    component.optionsFailed = ['accounts'];

    expect(component.isOptionLoading('journals')).toBe(true);
    expect(component.isOptionLoading('accounts')).toBe(false);
    expect(component.hasOptionFailed('accounts')).toBe(true);
    expect(component.hasOptionFailed(undefined)).toBe(false);
  });

  it('prevents submission while a requested option source is loading or failed', () => {
    const optionState = signal<'idle' | 'loading' | 'loaded' | 'failed'>('loading');
    const component = new RecordFormComponent({ status: () => optionState } as never);
    component.fields = [{ key: 'journalId', label: 'Journal', type: 'select', optionSource: 'journals' }];

    expect(component.hasPendingOptions()).toBe(true);
    expect(component.hasFailedOptions()).toBe(false);

    optionState.set('failed');
    expect(component.hasPendingOptions()).toBe(false);
    expect(component.hasFailedOptions()).toBe(true);
  });
});
