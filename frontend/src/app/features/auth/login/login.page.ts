import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth.service';
import { errorMessage } from '../../../shared/utils/page-utils';

@Component({
  selector: 'app-login-page',
  imports: [FormsModule, RouterLink],
  template: `
    <main class="auth-page">
      <section class="auth-brand">
        <a class="brand" routerLink="/login"><span class="brand-mark">G</span> gasoft</a>
        <div class="brand-copy">
          <p class="eyebrow">FINANCE, IN FOCUS</p>
          <h1>Good decisions<br />start with clear books.</h1>
          <p>One calm place to manage your accounts, operations, and reporting.</p>
        </div>
        <span class="brand-foot">Secure workspace for your business</span>
      </section>
      <section class="auth-form-wrap">
        <form class="auth-card" (ngSubmit)="submit()">
          <p class="eyebrow">WELCOME BACK</p>
          <h2>Sign in to Gasoft</h2>
          <p class="muted">Enter your workspace address and sign-in details.</p>
          @if (error) { <div class="alert error" role="alert">{{ error }}</div> }
          <label>Workspace address
            <input name="workspaceSlug" [(ngModel)]="workspaceSlug" autocomplete="organization" placeholder="your-workspace" required />
            <small>Use the address you chose when creating your workspace.</small>
          </label>
          <label>Email address<input name="email" type="email" [(ngModel)]="email" autocomplete="email" required /></label>
          <label>Password<input name="password" type="password" [(ngModel)]="password" autocomplete="current-password" required /></label>
          <p class="auth-forgot"><a routerLink="/forgot-password">Forgot password?</a></p>
          <button class="button primary full" [disabled]="busy">{{ busy ? 'Signing in…' : 'Sign in' }}</button>
          <p class="auth-switch">New to Gasoft? <a routerLink="/register">Create a workspace</a></p>
        </form>
      </section>
    </main>
  `,
})
export class LoginPage {
  email = '';
  password = '';
  workspaceSlug = '';
  busy = false;
  error = '';

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    route: ActivatedRoute,
  ) {
    this.workspaceSlug = auth.workspaceSlug();
    if (route.snapshot.queryParamMap.get('reason') === 'session-expired') {
      this.error = 'Your session expired. Please sign in again.';
    }
  }

  submit(): void {
    this.workspaceSlug = this.workspaceSlug.trim().toLowerCase();
    if (!this.workspaceSlug) {
      this.error = 'Enter your workspace address.';
      return;
    }
    this.busy = true;
    this.error = '';
    this.auth.login(this.email.trim(), this.password, this.workspaceSlug).subscribe({
      next: () => void this.router.navigate(['/dashboard']),
      error: error => {
        this.error = errorMessage(error, 'Sign-in failed. Check your workspace address, email, and password.');
        this.busy = false;
      },
    });
  }
}
