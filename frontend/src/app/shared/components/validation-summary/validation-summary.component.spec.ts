import { TestBed } from '@angular/core/testing';
import { FormValidationService } from '../../services/form-validation.service';
import { ValidationSummaryComponent } from './validation-summary.component';

describe('ValidationSummaryComponent', () => {
  it('formats nested server field paths for the validation summary', () => {
    TestBed.configureTestingModule({ providers: [FormValidationService] });
    const component = TestBed.runInInjectionContext(() => new ValidationSummaryComponent());

    expect(component.fieldLabel('lines[2].unitPrice')).toBe('Lines 2 · Unit Price');
    expect(component.fieldLabel('exchangeRate')).toBe('Exchange Rate');
  });
});
