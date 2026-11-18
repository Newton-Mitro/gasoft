import {
  Directive,
  DestroyRef,
  ElementRef,
  HostListener,
  Input,
  Renderer2,
  effect,
  inject,
  signal,
} from '@angular/core';
import { FormValidationService } from '../services/form-validation.service';

let nextFieldErrorId = 0;

@Directive({
  selector: 'input[name], app-input[name], select[name], textarea[name]',
  standalone: true,
})
export class FieldValidationDirective {
  private readonly element = inject(ElementRef<HTMLElement>);
  private readonly renderer = inject(Renderer2);
  private readonly validation = inject(FormValidationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fieldName = signal('');
  private unregisterField: (() => void) | null = null;
  private readonly errorElement = this.renderer.createElement('span') as HTMLSpanElement;
  private readonly errorId = `field-server-error-${nextFieldErrorId++}`;

  @Input()
  set name(value: string | null) {
    const field = value ?? '';
    if (field === this.fieldName()) return;
    this.unregisterField?.();
    this.fieldName.set(field);
    this.unregisterField = field ? this.validation.registerField(field) : null;
  }

  constructor() {
    this.renderer.addClass(this.errorElement, 'field-validation-error');
    if (this.element.nativeElement.tagName === 'APP-INPUT') {
      this.renderer.addClass(this.errorElement, 'app-input-validation-error');
    }
    this.renderer.setAttribute(this.errorElement, 'role', 'alert');
    this.renderer.setAttribute(this.errorElement, 'id', this.errorId);
    this.destroyRef.onDestroy(() => {
      this.unregisterField?.();
      if (this.errorElement.parentElement) {
        this.renderer.removeChild(this.errorElement.parentElement, this.errorElement);
      }
    });

    effect(() => {
      const field = this.fieldName();
      const messages = field ? this.validation.messagesFor(field) : [];
      this.render(messages);
    });
  }

  @HostListener('input')
  @HostListener('change')
  clearFieldError(): void {
    const field = this.fieldName();
    if (field) this.validation.clearField(field);
  }

  private render(messages: string[]): void {
    const control = this.element.nativeElement;
    const controlParent = control.parentElement;
    const searchableSelectWrapper =
      control.tagName === 'SELECT' && controlParent?.classList.contains('searchable-select')
        ? controlParent
        : null;
    const parent = searchableSelectWrapper?.parentElement ?? controlParent;
    if (!parent) return;
    const validationTarget = control.querySelector('input') ?? control;

    if (messages.length) {
      this.renderer.setAttribute(validationTarget, 'aria-invalid', 'true');
      const describedBy = (validationTarget.getAttribute('aria-describedby') ?? '')
        .split(/\s+/)
        .filter(Boolean)
        .filter((id: string) => id !== this.errorId);
      this.renderer.setAttribute(
        validationTarget,
        'aria-describedby',
        [...describedBy, this.errorId].join(' '),
      );
      this.renderer.setProperty(this.errorElement, 'textContent', messages.join(' '));
      if (!this.errorElement.parentElement) {
        const anchor = searchableSelectWrapper?.nextSibling ?? control.nextSibling;
        this.renderer.insertBefore(parent, this.errorElement, anchor);
      }
      return;
    }

    this.renderer.removeAttribute(validationTarget, 'aria-invalid');
    const describedBy = (validationTarget.getAttribute('aria-describedby') ?? '')
      .split(/\s+/)
      .filter((id: string) => id && id !== this.errorId);
    if (describedBy.length)
      this.renderer.setAttribute(validationTarget, 'aria-describedby', describedBy.join(' '));
    else this.renderer.removeAttribute(validationTarget, 'aria-describedby');
    if (this.errorElement.parentElement) {
      this.renderer.removeChild(this.errorElement.parentElement, this.errorElement);
    }
  }
}
