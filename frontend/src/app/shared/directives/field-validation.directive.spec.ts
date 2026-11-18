import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormValidationService } from '../services/form-validation.service';
import { FieldValidationDirective } from './field-validation.directive';
import { SearchableSelectDirective } from './searchable-select.directive';

@Component({
  standalone: true,
  imports: [FieldValidationDirective, SearchableSelectDirective],
  template: `
    <label><input [name]="fieldName" /></label>
    <label>
      <select appSearchableSelect name="account">
        <option value="">Select an account</option>
        <option value="1">Account</option>
      </select>
    </label>
  `,
})
class FieldValidationHostComponent {
  fieldName = 'email';
}

describe('FieldValidationDirective', () => {
  it('renders the matching server error beside its input and clears it when the input changes', () => {
    TestBed.configureTestingModule({ imports: [FieldValidationHostComponent] });
    const fixture = TestBed.createComponent(FieldValidationHostComponent);
    const validation = TestBed.inject(FormValidationService);
    fixture.detectChanges();

    validation.setErrors({ Email: ['Enter a valid email address.'] });
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    const error = fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement;
    expect(error.textContent).toBe('Enter a valid email address.');
    expect(input.nextElementSibling).toBe(error);
    expect(input.getAttribute('aria-invalid')).toBe('true');
    expect(input.getAttribute('aria-describedby')).toContain(error.id);

    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(validation.messagesFor('email')).toEqual([]);
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    expect(input.hasAttribute('aria-invalid')).toBe(false);
  });

  it('places searchable select errors beside the wrapper, matching input error layout', () => {
    TestBed.configureTestingModule({ imports: [FieldValidationHostComponent] });
    const fixture = TestBed.createComponent(FieldValidationHostComponent);
    const validation = TestBed.inject(FormValidationService);
    fixture.detectChanges();

    validation.setErrors({ Account: ['Choose an account.'] });
    fixture.detectChanges();

    const wrapper = fixture.nativeElement.querySelector('.searchable-select') as HTMLElement;
    const error = fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement;
    expect(wrapper.nextElementSibling).toBe(error);
    expect(error.parentElement).toBe(wrapper.parentElement);
    fixture.destroy();
  });
});
