import { Component, HostListener, inject } from '@angular/core';
import { KeyValuePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationStart, Router } from '@angular/router';
import { filter } from 'rxjs';
import { FormValidationService } from '../../services/form-validation.service';

@Component({
  selector: 'app-validation-summary',
  template: `
    @if (formValidation.unmatchedErrors() | keyvalue; as errors) {
      @if (errors.length) {
        <section class="validation-summary" role="alert" aria-live="assertive" aria-atomic="true">
          <div class="validation-summary-heading">
            <strong>Please review the highlighted information.</strong>
            <button type="button" aria-label="Dismiss validation messages" (click)="formValidation.clear()">×</button>
          </div>
          <ul>
            @for (error of errors; track error.key) {
              @for (message of error.value; track $index) {
                <li><span>{{ fieldLabel(error.key) }}:</span> {{ message }}</li>
              }
            }
          </ul>
        </section>
      }
    }
  `,
  styles: [`
    :host {
      position: fixed;
      z-index: 1100;
      inset: 18px 18px auto auto;
      width: min(440px, calc(100vw - 36px));
    }
    .validation-summary {
      padding: 14px 16px;
      border: 1px solid color-mix(in srgb, var(--error-text) 45%, var(--border));
      border-left: 4px solid var(--error-text);
      border-radius: 12px;
      background: var(--surface);
      color: var(--error-text);
      box-shadow: 0 12px 32px color-mix(in srgb, var(--text-strong) 18%, transparent);
      font-size: 12px;
    }
    .validation-summary-heading {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 12px;
    }
    .validation-summary-heading button {
      flex: 0 0 auto;
      border: 0;
      border-radius: 5px;
      background: transparent;
      color: var(--text-muted);
      cursor: pointer;
      font: inherit;
      font-size: 20px;
      line-height: 1;
    }
    ul {
      display: grid;
      gap: 5px;
      max-height: min(42vh, 300px);
      margin: 10px 0 0;
      padding-left: 18px;
      overflow: auto;
      color: var(--error-text);
    }
    li span {
      color: var(--error-text);
      font-weight: 650;
    }
    @media (max-width: 600px) {
      :host {
        inset: 12px 12px auto;
        width: auto;
      }
    }
  `],
  imports: [KeyValuePipe],
})
export class ValidationSummaryComponent {
  readonly formValidation = inject(FormValidationService);

  constructor() {
    inject(Router).events
      .pipe(
        filter(event => event instanceof NavigationStart),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.formValidation.clear());
  }

  @HostListener('input', ['$event.target'])
  @HostListener('change', ['$event.target'])
  clearChangedField(target: EventTarget | null): void {
    if (!(target instanceof HTMLInputElement || target instanceof HTMLSelectElement || target instanceof HTMLTextAreaElement)) return;
    if (target.name) this.formValidation.clearField(target.name);
  }

  fieldLabel(key: string): string {
    const path = key.replace(/\[(\d+)\]/g, ' $1').split('.');
    return path.map(part => part
      .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
      .replace(/[_-]+/g, ' ')
      .replace(/\b\w/g, character => character.toUpperCase()))
      .join(' · ');
  }
}
