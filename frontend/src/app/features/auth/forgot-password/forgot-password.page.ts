import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth.service';
import { errorMessage } from '../../../shared/utils/page-utils';

@Component({
  selector: 'app-forgot-password-page',
  imports: [FormsModule, RouterLink],
  template: `
    <main class="auth-page">
      <section class="auth-brand">
        <a class="brand" routerLink="/login"><span class="brand-mark">G</span> gasoft</a>
        <div class="brand-copy">
          <p class="eyebrow">ACCOUNT RECOVERY</p>
          <h1>Back to business,<br />securely.</h1>
          <p>We’ll send a one-time password reset link if the account matches your workspace.</p>
        </div>
        <span class="brand-foot">Secure workspace for your business</span>
      </section>
      <section class="auth-form-wrap">
        <form class="auth-card" (ngSubmit)="submit()">
          <p class="eyebrow">RESET PASSWORD</p>
          <h2>Forgot your password?</h2>
          <p class="muted">Enter the email linked to your account.</p>
          @if (error()) { <div class="alert error" role="alert">{{ error() }}</div> }
          @if (sent()) {
            <div class="alert success" role="status" aria-live="polite">
              If an account matches those details, a password reset link will be sent shortly. Check your inbox.
            </div>
          } @else {
            <label>Email address
              <input name="email" type="email" [(ngModel)]="email" autocomplete="email" required />
            </label>
            <button class="button primary full" [disabled]="busy()">{{ busy() ? 'Sending…' : 'Send reset link' }}</button>
          }
          <p class="auth-switch"><a routerLink="/login">Back to sign in</a></p>
        </form>
      </section>
    </main>
  `,
})
export class ForgotPasswordPage {
  readonly busy = signal(false);
  readonly sent = signal(false);
  readonly error = signal('');
  email = '';

  constructor(private readonly auth: AuthService) {}

  submit(): void {
    this.busy.set(true);
    this.error.set('');
    this.auth.requestPasswordReset(this.email.trim()).subscribe({
      next: () => {
        this.sent.set(true);
        this.busy.set(false);
      },
      error: error => {
        this.error.set(errorMessage(error, 'Could not request a password reset. Please try again.'));
        this.busy.set(false);
      },
    });
  }
}
