import { booleanAttribute, Component, forwardRef, Input, OnChanges } from '@angular/core';
import {
  AbstractControl,
  ControlValueAccessor,
  NG_VALIDATORS,
  NG_VALUE_ACCESSOR,
  ValidationErrors,
  Validator,
} from '@angular/forms';

type InputType =
  | 'text'
  | 'password'
  | 'email'
  | 'tel'
  | 'url'
  | 'date'
  | 'number'
  | 'checkbox'
  | 'radio'
  | 'search';

@Component({
  selector: 'app-input',
  template: `
    <input
      [class]="inputClass + ' app-input-control'"
      [attr.id]="id || null"
      [attr.name]="name || null"
      [type]="type"
      [value]="type === 'checkbox' ? '' : (value ?? '')"
      [checked]="
        type === 'checkbox'
          ? checked || modelValue === true
          : type === 'radio' && value === modelValue
      "
      [attr.placeholder]="placeholder || null"
      [required]="required"
      [disabled]="disabled"
      [readOnly]="readonly"
      [attr.autocomplete]="autocomplete || null"
      [attr.inputmode]="inputmode || null"
      [attr.min]="min ?? null"
      [attr.max]="max ?? null"
      [attr.minlength]="minlength ?? null"
      [attr.maxlength]="maxlength ?? null"
      [attr.pattern]="pattern || null"
      [attr.step]="step ?? null"
      [attr.aria-label]="ariaLabel || null"
      [attr.aria-describedby]="ariaDescribedby || null"
      [attr.accept]="accept || null"
      (input)="onInput($event)"
      (change)="onChangeEvent($event)"
      (blur)="onTouched()"
    />
  `,
  styles: `
    :host {
      display: contents;
    }

    .app-input-control:not([type='checkbox']):not([type='radio']) {
      box-sizing: border-box;
      padding: 0 12px;
      border: 1px solid var(--border);
      border-radius: 8px;
      background: var(--surface);
      color: var(--text-strong);
      font: inherit;
      text-align: left;
      transition:
        border-color 0.15s ease,
        box-shadow 0.15s ease,
        background 0.15s ease;
    }

    :host-context(.field-grid) .app-input-control:not([type='checkbox']):not([type='radio']) {
      width: 100%;
      height: 44px;
      font-size: inherit;
    }

    :host-context(.header-fields) .app-input-control:not([type='checkbox']):not([type='radio']),
    :host-context(.editable-line) .app-input-control:not([type='checkbox']):not([type='radio']),
    :host-context(.receive-fields) .app-input-control:not([type='checkbox']):not([type='radio']) {
      width: 100%;
      min-width: 0;
      height: 38px;
    }

    :host-context(.member-fields) .app-input-control:not([type='checkbox']):not([type='radio']) {
      width: 100%;
      min-width: 0;
      height: 42px;
    }

    :host-context(.audit-filters) .app-input-control:not([type='checkbox']):not([type='radio']) {
      width: 100%;
      min-width: 0;
      height: 40px;
    }

    :host-context(.document-line) .app-input-control:not([type='checkbox']):not([type='radio']) {
      width: 100%;
      min-width: 0;
      height: 34px;
    }

    :host-context(.journal-line) .app-input-control:not([type='checkbox']):not([type='radio']) {
      width: 100%;
      min-width: 0;
      height: 36px;
    }

    :host-context(.account-panel) .app-input-control:not([type='checkbox']):not([type='radio']) {
      width: 100%;
      height: 39px;
      padding: 0 11px;
      border-radius: 6px;
    }

    :host-context(.rate-form) .app-input-control:not([type='checkbox']):not([type='radio']) {
      width: 100%;
      height: 40px;
      padding: 0 11px;
    }

    :host-context(.aging-filter) .app-input-control:not([type='checkbox']):not([type='radio']),
    :host-context(.cash-flow-filter) .app-input-control:not([type='checkbox']):not([type='radio']),
    :host-context(.report-filters) .app-input-control:not([type='checkbox']):not([type='radio']) {
      height: 37px;
      padding: 0 10px;
    }

    :host-context(.account-form) .app-input-control:not([type='checkbox']):not([type='radio']) {
      width: 100%;
      min-width: 0;
      min-height: 40px;
      padding: 0 11px;
    }

    :host-context(.asset-fields) .app-input-control:not([type='checkbox']):not([type='radio']) {
      width: 100%;
      min-width: 0;
      min-height: 36px;
      padding: 0 9px;
      border-radius: 7px;
    }

    :host-context(.budget-toolbar) .app-input-control:not([type='checkbox']):not([type='radio']),
    :host-context(.plan-table) .app-input-control:not([type='checkbox']):not([type='radio']) {
      min-height: 36px;
      padding: 0 9px;
      border-radius: 7px;
    }

    :host-context(.plan-table) .app-input-control:not([type='checkbox']):not([type='radio']) {
      width: 115px;
      text-align: right;
    }

    :host-context(.form-grid) .app-input-control:not([type='checkbox']):not([type='radio']),
    :host-context(.run-controls) .app-input-control:not([type='checkbox']):not([type='radio']) {
      min-width: 0;
      min-height: 38px;
      padding: 0 10px;
    }

    :host-context(.translation-rates) .app-input-control:not([type='checkbox']):not([type='radio']),
    :host-context(.run-adjustments) .app-input-control:not([type='checkbox']):not([type='radio']) {
      width: 100%;
      min-width: 90px;
      min-height: 32px;
      padding: 0 8px;
      border-radius: 6px;
    }

    :host-context(.allocation-input) .app-input-control:not([type='checkbox']):not([type='radio']),
    :host-context(.adjustment-form) .app-input-control:not([type='checkbox']):not([type='radio']) {
      min-width: 90px;
      min-height: 32px;
      padding: 0 8px;
      border-radius: 6px;
    }

    .app-input-control.quantity-input {
      width: 130px;
      height: 38px;
      padding: 0 10px;
      border-radius: 6px;
    }

    .app-input-control:not([type='checkbox']):not([type='radio']):hover:not(:disabled) {
      border-color: var(--focus-color);
    }

    .app-input-control:not([type='checkbox']):not([type='radio']):focus-visible {
      border-color: var(--focus-color);
      outline: 0;
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--focus-color) 17%, transparent);
    }

    .app-input-control:not([type='checkbox']):not([type='radio']):disabled {
      background: var(--surface-muted);
      color: var(--text-muted);
      cursor: not-allowed;
    }

    .app-input-control:not([type='checkbox']):not([type='radio'])[aria-invalid='true'] {
      border-color: var(--error-text) !important;
    }

    .app-input-control:not([type='checkbox']):not([type='radio'])::placeholder {
      color: var(--text-muted);
    }
  `,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputComponent),
      multi: true,
    },
    {
      provide: NG_VALIDATORS,
      useExisting: forwardRef(() => InputComponent),
      multi: true,
    },
  ],
})
export class InputComponent implements ControlValueAccessor, Validator, OnChanges {
  @Input() id = '';
  @Input() name = '';
  @Input() type: InputType = 'text';
  @Input() placeholder = '';
  @Input() autocomplete = '';
  @Input() inputmode = '';
  @Input() min: number | string | null = null;
  @Input() max: number | string | null = null;
  @Input() minlength: number | string | null = null;
  @Input() maxlength: number | string | null = null;
  @Input() pattern = '';
  @Input() step: number | string | null = null;
  @Input() accept = '';
  @Input({ alias: 'readonly', transform: booleanAttribute }) readonly = false;
  @Input({ transform: booleanAttribute }) required = false;
  @Input({ transform: booleanAttribute }) disabled = false;
  @Input({ transform: booleanAttribute }) checked = false;
  @Input('aria-label') ariaLabel = '';
  @Input('aria-describedby') ariaDescribedby = '';
  @Input('class') inputClass = '';
  @Input() value: unknown = '';

  private onChange: (value: unknown) => void = () => {};
  private onValidatorChange: () => void = () => {};
  modelValue: unknown = '';
  onTouched: () => void = () => {};

  writeValue(value: unknown): void {
    this.modelValue = value ?? (this.type === 'checkbox' ? false : '');
    if (this.type !== 'radio') this.value = this.modelValue;
  }

  registerOnChange(fn: (value: unknown) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(disabled: boolean): void {
    this.disabled = disabled;
  }

  ngOnChanges(): void {
    this.onValidatorChange();
  }

  validate(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    const empty = value === null || value === undefined || value === '';
    if (this.required && (empty || (this.type === 'checkbox' && value !== true))) {
      return { required: true };
    }
    if (empty) return null;

    const text = String(value);
    if (this.minlength !== null && text.length < Number(this.minlength)) {
      return { minlength: { requiredLength: Number(this.minlength), actualLength: text.length } };
    }
    if (this.maxlength !== null && text.length > Number(this.maxlength)) {
      return { maxlength: { requiredLength: Number(this.maxlength), actualLength: text.length } };
    }
    if (this.pattern && !new RegExp(`^(?:${this.pattern})$`).test(text)) {
      return { pattern: { requiredPattern: this.pattern, actualValue: text } };
    }
    if (this.type === 'number') {
      const number = Number(value);
      if (this.min !== null && number < Number(this.min)) {
        return { min: { min: Number(this.min), actual: number } };
      }
      if (this.max !== null && number > Number(this.max)) {
        return { max: { max: Number(this.max), actual: number } };
      }
    }
    return null;
  }

  registerOnValidatorChange(fn: () => void): void {
    this.onValidatorChange = fn;
  }

  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (this.type === 'checkbox') return;
    const value =
      this.type === 'number' ? (input.value === '' ? null : input.valueAsNumber) : input.value;
    this.updateValue(value);
  }

  onChangeEvent(event: Event): void {
    if (this.type === 'checkbox') {
      this.updateValue((event.target as HTMLInputElement).checked);
    } else if (this.type === 'radio') {
      this.updateValue(this.value);
    }
  }

  private updateValue(value: unknown): void {
    this.modelValue = value;
    if (this.type !== 'radio') this.value = value;
    this.onChange(value);
  }
}
