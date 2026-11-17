import { signal } from '@angular/core';
import { RecordFormComponent } from './record-form.component';
import { FormValidationService } from '../../services/form-validation.service';

describe('RecordFormComponent option loading', () => {
  it('identifies option sources that are loading or failed', () => {
    const formValidation = new FormValidationService();
    const optionData = { status: () => signal<'idle' | 'loading' | 'loaded' | 'failed'>('idle') };
    const component = new RecordFormComponent(optionData as never, formValidation);
    component.optionsLoading = ['journals'];
    component.optionsFailed = ['accounts'];

    expect(component.isOptionLoading('journals')).toBe(true);
    expect(component.isOptionLoading('accounts')).toBe(false);
    expect(component.hasOptionFailed('accounts')).toBe(true);
    expect(component.hasOptionFailed(undefined)).toBe(false);
  });

  it('prevents submission while a requested option source is loading or failed', () => {
    const formValidation = new FormValidationService();
    const optionState = signal<'idle' | 'loading' | 'loaded' | 'failed'>('loading');
    const component = new RecordFormComponent({ status: () => optionState } as never, formValidation);
    component.fields = [{ key: 'journalId', label: 'Journal', type: 'select', optionSource: 'journals' }];

    expect(component.hasPendingOptions()).toBe(true);
    expect(component.hasFailedOptions()).toBe(false);

    optionState.set('failed');
    expect(component.hasPendingOptions()).toBe(false);
    expect(component.hasFailedOptions()).toBe(true);
  });

  it('shows and clears field-level server validation messages', () => {
    const component = new RecordFormComponent(
      { status: () => signal<'idle' | 'loading' | 'loaded' | 'failed'>('idle') } as never,
      new FormValidationService(),
    );
    component.formValidation.setErrors({ Email: ['Enter a valid email address.'] });

    expect(component.fieldErrors('email')).toEqual(['Enter a valid email address.']);
    component.clearFieldError('email');
    expect(component.fieldErrors('email')).toEqual([]);
  });

  it('blocks submission and marks missing required fields inline', () => {
    const component = new RecordFormComponent(
      { status: () => signal<'idle' | 'loading' | 'loaded' | 'failed'>('idle') } as never,
      new FormValidationService(),
    );
    component.fields = [
      { key: 'fiscalYearId', label: 'Fiscal year', type: 'select', required: true },
      { key: 'startsOn', label: 'Starts on', type: 'date', required: true },
    ];
    const submit = vi.spyOn(component.submitForm, 'emit');

    component.submit();

    expect(submit).not.toHaveBeenCalled();
    expect(component.fieldErrors('fiscalYearId')).toEqual(['Select fiscal year.']);
    expect(component.fieldErrors('startsOn')).toEqual(['Enter a valid starts on date.']);
  });

  it('rejects invalid numeric IDs and out-of-range values before emitting', () => {
    const component = new RecordFormComponent(
      { status: () => signal<'idle' | 'loading' | 'loaded' | 'failed'>('idle') } as never,
      new FormValidationService(),
    );
    component.fields = [
      { key: 'fiscalYearId', label: 'Fiscal year', type: 'select', required: true },
      { key: 'pageSize', label: 'Page size', type: 'number', required: true, min: 1, max: 100 },
    ];
    component.form = { fiscalYearId: null, pageSize: 101 };
    const submit = vi.spyOn(component.submitForm, 'emit');

    component.submit();

    expect(submit).not.toHaveBeenCalled();
    expect(component.fieldErrors('fiscalYearId')).toEqual(['Select fiscal year.']);
    expect(component.fieldErrors('pageSize')).toEqual(['Page size must be no more than 100.']);
  });
});
