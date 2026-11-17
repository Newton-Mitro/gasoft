import { TestBed } from '@angular/core/testing';
import { NavigationStart, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { FormValidationService } from '../../services/form-validation.service';
import { ValidationSummaryComponent } from './validation-summary.component';

describe('ValidationSummaryComponent', () => {
  it('formats nested server field paths for the validation summary', () => {
    TestBed.configureTestingModule({ providers: [FormValidationService] });
    const component = TestBed.runInInjectionContext(() => new ValidationSummaryComponent());

    expect(component.fieldLabel('lines[2].unitPrice')).toBe('Lines 2 · Unit Price');
    expect(component.fieldLabel('exchangeRate')).toBe('Exchange Rate');
  });

  it('clears stale validation errors when navigation starts', () => {
    const events = new Subject<unknown>();
    TestBed.configureTestingModule({
      providers: [
        FormValidationService,
        { provide: Router, useValue: { events } },
      ],
    });
    const validation = TestBed.inject(FormValidationService);
    TestBed.runInInjectionContext(() => new ValidationSummaryComponent());
    validation.setErrors({ email: ['Enter a valid email.'] });

    events.next(new NavigationStart(1, '/dashboard'));

    expect(validation.errors()).toEqual({});
  });
});
