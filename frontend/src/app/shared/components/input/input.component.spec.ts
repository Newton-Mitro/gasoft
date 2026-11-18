import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TestBed } from '@angular/core/testing';
import { FormValidationService } from '../../services/form-validation.service';
import { FieldValidationDirective } from '../../directives/field-validation.directive';
import { InputComponent } from './input.component';

@Component({
  imports: [FormsModule, InputComponent],
  template: `
    <label>
      <app-input name="memo" required [(ngModel)]="memo" />
    </label>
    <app-input name="amount" type="number" [(ngModel)]="amount" />
    <app-input name="enabled" type="checkbox" [(ngModel)]="enabled" />
    <app-input
      name="search"
      class="search-control"
      type="search"
      [value]="search"
      (input)="lastInput = $any($event.target).value"
    />
    <app-input
      name="tenant"
      type="radio"
      value="first"
      [(ngModel)]="selectedTenant"
      [ngModelOptions]="standaloneModel"
    />
    <app-input
      name="tenant"
      type="radio"
      value="second"
      [(ngModel)]="selectedTenant"
      [ngModelOptions]="standaloneModel"
    />
  `,
})
class InputHostComponent {
  memo = '';
  amount: number | null = 2;
  enabled = false;
  search = 'cash';
  lastInput = '';
  selectedTenant = 'first';
  standaloneModel = { standalone: true };
}

@Component({
  imports: [InputComponent, FieldValidationDirective],
  template: '<label><app-input name="memo" /></label>',
})
class InputValidationHostComponent {}

@Component({
  imports: [InputComponent],
  template:
    '<div class="field-grid" style="font-size: 11px"><label><app-input type="date" /></label></div>',
})
class FieldGridInputHostComponent {}

@Component({
  imports: [InputComponent],
  template: `
    <div class="header-fields">
      <label><app-input type="date" /></label>
    </div>
    <div class="editable-line">
      <label><app-input type="number" /></label>
    </div>
  `,
})
class CompactFormInputHostComponent {}

@Component({
  imports: [InputComponent],
  template: '<div class="member-fields"><label><app-input type="email" /></label></div>',
})
class MemberFieldsInputHostComponent {}

@Component({
  imports: [InputComponent],
  template: `
    <fieldset class="document-line"><app-input /></fieldset>
    <div class="journal-line"><app-input /></div>
    <section class="account-panel"><app-input /></section>
    <form class="rate-form"><app-input /></form>
    <section class="aging-filter"><app-input /></section>
    <div class="asset-fields"><app-input /></div>
    <div class="account-form"><app-input /></div>
    <table class="translation-rates">
      <tr>
        <td><app-input /></td>
      </tr>
    </table>
    <div class="budget-toolbar"><app-input /></div>
    <div class="form-grid"><app-input /></div>
    <div class="run-controls"><app-input /></div>
    <table class="run-adjustments">
      <tr>
        <td><app-input /></td>
      </tr>
    </table>
    <label class="allocation-input"><app-input /></label>
    <app-input class="quantity-input" />
  `,
})
class AdditionalFormInputHostComponent {}

describe('InputComponent', () => {
  it('matches the field-grid searchable select dimensions', () => {
    TestBed.configureTestingModule({ imports: [FieldGridInputHostComponent] });
    const fixture = TestBed.createComponent(FieldGridInputHostComponent);
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    const styles = getComputedStyle(input);
    expect(styles.width).toBe('100%');
    expect(styles.height).toBe('44px');
    expect(styles.padding).toBe('0px 12px');
    expect(styles.fontSize).toBe('11px');
    expect(styles.borderRadius).toBe('8px');
  });

  it('matches searchable-select dimensions in compact order forms', () => {
    TestBed.configureTestingModule({ imports: [CompactFormInputHostComponent] });
    const fixture = TestBed.createComponent(CompactFormInputHostComponent);
    fixture.detectChanges();

    const inputs = fixture.nativeElement.querySelectorAll('input') as NodeListOf<HTMLInputElement>;
    for (const input of inputs) {
      const styles = getComputedStyle(input);
      expect(styles.height).toBe('38px');
      expect(styles.padding).toBe('0px 12px');
      expect(styles.borderRadius).toBe('8px');
    }
  });

  it('matches searchable-select height in the team-member form', () => {
    TestBed.configureTestingModule({ imports: [MemberFieldsInputHostComponent] });
    const fixture = TestBed.createComponent(MemberFieldsInputHostComponent);
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    expect(getComputedStyle(input).height).toBe('42px');
  });

  it('keeps additional page-specific inputs at their intended dimensions', () => {
    TestBed.configureTestingModule({ imports: [AdditionalFormInputHostComponent] });
    const fixture = TestBed.createComponent(AdditionalFormInputHostComponent);
    fixture.detectChanges();

    const inputs = Array.from(
      fixture.nativeElement.querySelectorAll('input'),
    ) as HTMLInputElement[];
    expect(getComputedStyle(inputs[0]).height).toBe('34px');
    expect(getComputedStyle(inputs[1]).height).toBe('36px');
    expect(getComputedStyle(inputs[2]).height).toBe('39px');
    expect(getComputedStyle(inputs[3]).height).toBe('40px');
    expect(getComputedStyle(inputs[4]).height).toBe('37px');
    expect(getComputedStyle(inputs[5]).minHeight).toBe('36px');
    expect(getComputedStyle(inputs[6]).minHeight).toBe('40px');
    expect(getComputedStyle(inputs[7]).minHeight).toBe('32px');
    expect(getComputedStyle(inputs[8]).minHeight).toBe('36px');
    expect(getComputedStyle(inputs[9]).minHeight).toBe('38px');
    expect(getComputedStyle(inputs[10]).minHeight).toBe('38px');
    expect(getComputedStyle(inputs[11]).minHeight).toBe('32px');
    expect(getComputedStyle(inputs[12]).minHeight).toBe('32px');
    expect(getComputedStyle(inputs[13]).width).toBe('130px');
    expect(getComputedStyle(inputs[13]).height).toBe('38px');
  });

  it('updates text, number, and checkbox models through Angular forms', async () => {
    TestBed.configureTestingModule({ imports: [InputHostComponent] });
    const fixture = TestBed.createComponent(InputHostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const inputs = fixture.nativeElement.querySelectorAll('input') as NodeListOf<HTMLInputElement>;
    const [memo, amount, enabled] = Array.from(inputs);

    memo.value = 'Invoice memo';
    memo.dispatchEvent(new Event('input', { bubbles: true }));
    amount.value = '12.5';
    amount.dispatchEvent(new Event('input', { bubbles: true }));
    enabled.checked = true;
    enabled.dispatchEvent(new Event('change', { bubbles: true }));
    fixture.detectChanges();

    expect(fixture.componentInstance.memo).toBe('Invoice memo');
    expect(fixture.componentInstance.amount).toBe(12.5);
    expect(fixture.componentInstance.enabled).toBe(true);
    expect(memo.required).toBe(true);
    expect(memo.placeholder).toBe('');

    const radios = fixture.nativeElement.querySelectorAll(
      'input[type="radio"]',
    ) as NodeListOf<HTMLInputElement>;
    expect(radios[0].checked).toBe(true);
    radios[1].click();
    fixture.detectChanges();
    expect(fixture.componentInstance.selectedTenant).toBe('second');
    expect(radios[1].checked).toBe(true);
  });

  it('preserves native input events and supports a value-bound search control', async () => {
    TestBed.configureTestingModule({ imports: [InputHostComponent] });
    const fixture = TestBed.createComponent(InputHostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const search = fixture.nativeElement.querySelector('input[type="search"]') as HTMLInputElement;
    expect(search.value).toBe('cash');
    expect(search.classList.contains('search-control')).toBe(true);

    search.value = 'bank';
    search.dispatchEvent(new Event('input', { bubbles: true }));

    expect(fixture.componentInstance.lastInput).toBe('bank');
  });

  it('connects server field validation to the wrapped native input', () => {
    TestBed.configureTestingModule({ imports: [InputValidationHostComponent] });
    const fixture = TestBed.createComponent(InputValidationHostComponent);
    const validation = TestBed.inject(FormValidationService);
    fixture.detectChanges();

    validation.setErrors({ memo: ['The memo is invalid.'] });
    fixture.detectChanges();

    const host = fixture.nativeElement.querySelector('app-input') as HTMLElement;
    const input = host.querySelector('input') as HTMLInputElement;
    const error = fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement;
    expect(input.getAttribute('aria-invalid')).toBe('true');
    expect(input.getAttribute('aria-describedby')).toContain(error.id);
    expect(error.classList.contains('app-input-validation-error')).toBe(true);

    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();

    expect(validation.messagesFor('memo')).toEqual([]);
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    expect(input.hasAttribute('aria-invalid')).toBe(false);
  });
});
