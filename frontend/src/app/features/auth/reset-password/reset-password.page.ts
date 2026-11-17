import { Component, signal, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth.service';
import { errorMessage } from '../../../shared/utils/page-utils';
import { NotificationService } from '../../../shared/services/notification.service';
import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';

@Component({
  selector: 'app-reset-password-page',
  imports: [FormsModule, RouterLink, FieldValidationDirective],
  template: `
    <main class="auth-page">
      <section class="auth-brand">
        <a class="brand" routerLink="/login"><img class="brand-logo" src="/logo.png" alt="gasoft logo" /> gasoft</a>
        <div class="brand-copy">
          <p class="eyebrow">ACCOUNT RECOVERY</p>
          <h1>A fresh start,<br />with peace of mind.</h1>
          <p>Choose a new password to secure your Gasoft account.</p>
        </div>
        <span class="brand-foot">Your records stay yours</span>
      </section>
      <section class="auth-form-wrap">
        <form class="auth-card" (ngSubmit)="submit()">
          <p class="eyebrow">NEW PASSWORD</p>
          <h2>Reset your password</h2>
          <p class="muted">Use at least 12 characters with a number and a symbol.</p>
          @if (error()) { <div class="alert error" role="alert">{{ error() }}</div> }
          @if (success()) {
            <div class="alert success" role="status">{{ success() }}</div>
            <p class="auth-switch"><a routerLink="/login">Continue to sign in</a></p>
          } @else if (!email || !token) {
            <div class="alert error" role="alert">This reset link is incomplete. Request a new one to continue.</div>
            <p class="auth-switch"><a routerLink="/forgot-password">Request another reset link</a></p>
          } @else {
            <label>Email address
              <input name="email" type="email" [ngModel]="email" readonly autocomplete="email" />
            </label>
            <label>New password
              <input name="newPassword" type="password" [(ngModel)]="newPassword" minlength="12" autocomplete="new-password" required />
            </label>
            <label>Confirm new password
              <input name="confirmPassword" type="password" [(ngModel)]="confirmPassword" minlength="12" autocomplete="new-password" required />
            </label>
            <button class="button primary full" [disabled]="busy()">{{ busy() ? 'Updating…' : 'Update password' }}</button>
          }
        </form>
      </section>
    </main>
  `,
})
export class ResetPasswordPage {
  readonly notifications = inject(NotificationService);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly email: string;
  readonly token: string;
  newPassword = '';
  confirmPassword = '';

  constructor(route: ActivatedRoute, private readonly auth: AuthService) {
    this.email = route.snapshot.queryParamMap.get('email') ?? '';
    this.token = route.snapshot.queryParamMap.get('token') ?? '';
  }

  submit(): void {
    this.error.set('');
    if (this.newPassword !== this.confirmPassword) {
      this.error.set('The passwords do not match.');
      return;
    }

    this.busy.set(true);
    this.auth.resetPassword(this.email, this.token, this.newPassword).subscribe({
      next: () => {
        this.notifications.success('Your password has been updated. You can now sign in with your new password.');
        this.success.set('');
        this.newPassword = '';
        this.confirmPassword = '';
        this.busy.set(false);
      },
      error: error => {
        this.error.set('');
        this.notifications.error(errorMessage(error, 'Could not reset your password. Request a new link and try again.'));
        this.busy.set(false);
      },
    });
  }
}
