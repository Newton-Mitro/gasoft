import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService, WorkspaceOption } from '../../../core/auth.service';
import { errorMessage } from '../../../shared/utils/page-utils';

@Component({
  selector: 'app-login-page',
  imports: [FormsModule, RouterLink],
  styles: [`
    .auth-card {
      width: min(100%, 440px);
    }

    .auth-card h2 {
      margin-bottom: 8px;
      font-size: 30px;
      letter-spacing: -0.9px;
    }

    .auth-intro {
      margin: 0 0 24px;
      color: var(--text-muted);
      font-size: 13px;
      line-height: 1.6;
    }

    .workspace-list {
      display: grid;
      gap: 10px;
      margin: 0 0 18px;
    }

    .workspace-choice {
      position: relative;
      display: flex;
      flex-direction: row;
      align-items: center;
      gap: 13px;
      min-height: 70px;
      margin: 0;
      padding: 12px 15px;
      border: 1px solid var(--border);
      border-radius: 10px;
      background: var(--surface);
      color: var(--text-strong);
      cursor: pointer;
      transition: border-color .16s ease, background-color .16s ease, box-shadow .16s ease, transform .16s ease;
    }

    .workspace-choice input {
      position: absolute;
      width: 1px;
      height: 1px;
      opacity: 0;
      pointer-events: none;
    }

    .workspace-choice:hover {
      border-color: var(--focus-color);
      transform: translateY(-1px);
    }

    .workspace-choice:focus-within {
      outline: 3px solid color-mix(in srgb, var(--focus-color) 20%, transparent);
      outline-offset: 2px;
    }

    .workspace-choice.selected {
      border-color: var(--focus-color);
      background: color-mix(in srgb, var(--focus-color) 6%, var(--surface));
      box-shadow: 0 0 0 1px var(--focus-color);
    }

    .workspace-avatar {
      display: grid;
      flex: 0 0 42px;
      width: 42px;
      height: 42px;
      place-items: center;
      border-radius: 9px;
      background: color-mix(in srgb, var(--focus-color) 12%, var(--surface));
      color: var(--accent-text);
      font-size: 15px;
      font-weight: 750;
    }

    .workspace-copy {
      display: grid;
      flex: 1;
      min-width: 0;
      gap: 4px;
    }

    .workspace-name {
      overflow: hidden;
      color: var(--text-strong);
      font-size: 13px;
      font-weight: 650;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .workspace-caption {
      color: var(--text-muted);
      font-size: 11px;
      font-weight: 400;
    }

    .workspace-indicator {
      display: grid;
      flex: 0 0 21px;
      width: 21px;
      height: 21px;
      place-items: center;
      border: 1px solid var(--border);
      border-radius: 50%;
      color: transparent;
      font-size: 12px;
      font-weight: 700;
    }

    .workspace-choice.selected .workspace-indicator {
      border-color: var(--focus-color);
      background: var(--focus-color);
      color: #fff;
    }

    .auth-card .workspace-submit {
      height: 46px;
      margin-top: 0;
      border-radius: 8px;
      font-size: 13px;
      font-weight: 650;
    }

    .workspace-actions {
      margin-top: 15px;
      text-align: center;
    }

    .workspace-back {
      padding: 6px 10px;
      border: 0;
      background: transparent;
      color: var(--text-muted);
      font: inherit;
      font-size: 11px;
      cursor: pointer;
    }

    .workspace-back:hover {
      color: var(--accent-text);
      text-decoration: underline;
    }

    .auth-card .workspace-footer {
      margin-top: 16px;
    }
  `],
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
          <h2>{{ availableWorkspaces ? 'Choose a workspace' : 'Sign in to Gasoft' }}</h2>
          <p class="auth-intro">
            {{ availableWorkspaces ? 'Select the workspace you want to open.' : 'Enter your email and password to continue.' }}
          </p>
          @if (error) { <div class="alert error" role="alert">{{ error }}</div> }
          @if (!availableWorkspaces) {
            <label>Email address<input name="email" type="email" [(ngModel)]="email" autocomplete="email" required /></label>
            <label>Password<input name="password" type="password" [(ngModel)]="password" autocomplete="current-password" required /></label>
            <p class="auth-forgot"><a routerLink="/forgot-password">Forgot password?</a></p>
            <button class="button primary full" [disabled]="busy">{{ busy ? 'Signing in…' : 'Continue' }}</button>
          } @else {
            <div class="workspace-list" role="radiogroup" aria-label="Available workspaces">
              @for (workspace of availableWorkspaces; track workspace.tenantId) {
                <label class="workspace-choice" [class.selected]="selectedTenantId === workspace.tenantId">
                <input
                  type="radio"
                  name="tenantId"
                  [value]="workspace.tenantId"
                  [(ngModel)]="selectedTenantId"
                  required
                />
                  <span class="workspace-avatar" aria-hidden="true">{{ workspace.name.charAt(0).toUpperCase() }}</span>
                  <span class="workspace-copy">
                    <span class="workspace-name">{{ workspace.name }}</span>
                    <span class="workspace-caption">Workspace</span>
                  </span>
                  <span class="workspace-indicator" aria-hidden="true"><i class="fa-solid fa-check"></i></span>
                </label>
              }
            </div>
            <button class="button primary full workspace-submit" [disabled]="busy || selectedTenantId === null">
              {{ busy ? 'Opening workspace…' : 'Continue to workspace' }}
            </button>
            <div class="workspace-actions">
              <button class="workspace-back" type="button" [disabled]="busy" (click)="useDifferentAccount()">
                <i class="fa-solid fa-arrow-left" aria-hidden="true"></i> Use a different account
              </button>
            </div>
          }
          <p class="auth-switch" [class.workspace-footer]="availableWorkspaces">
            {{ availableWorkspaces ? 'Need another workspace?' : 'New to Gasoft?' }}
            <a routerLink="/register">{{ availableWorkspaces ? 'Create one' : 'Create a workspace' }}</a>
          </p>
        </form>
      </section>
    </main>
  `,
})
export class LoginPage {
  email = '';
  password = '';
  availableWorkspaces: WorkspaceOption[] | null = null;
  selectedTenantId: number | null = null;
  busy = false;
  error = '';

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    route: ActivatedRoute,
  ) {
    if (route.snapshot.queryParamMap.get('reason') === 'session-expired') {
      this.error = 'Your session expired. Please sign in again.';
    }
  }

  submit(): void {
    if (this.availableWorkspaces) {
      if (this.selectedTenantId === null) {
        this.error = 'Choose a workspace to continue.';
        return;
      }
      this.openWorkspace(this.selectedTenantId);
      return;
    }

    this.busy = true;
    this.error = '';
    this.auth.getSignInWorkspaces(this.email.trim(), this.password).subscribe({
      next: workspaces => {
        this.availableWorkspaces = workspaces;
        this.selectedTenantId = workspaces.length === 1 ? workspaces[0].tenantId : null;
        if (workspaces.length === 1) this.openWorkspace(workspaces[0].tenantId);
        else this.busy = false;
      },
      error: error => {
        this.error = errorMessage(error, 'Sign-in failed. Check your email and password.');
        this.busy = false;
      },
    });
  }

  useDifferentAccount(): void {
    this.availableWorkspaces = null;
    this.selectedTenantId = null;
    this.password = '';
    this.error = '';
  }

  private openWorkspace(tenantId: number): void {
    this.busy = true;
    this.error = '';
    this.auth.login(this.email.trim(), this.password, tenantId).subscribe({
      next: () => void this.router.navigate(['/dashboard']),
      error: error => {
        this.error = errorMessage(error, 'Could not open that workspace. Please sign in again.');
        this.busy = false;
        this.availableWorkspaces = null;
        this.selectedTenantId = null;
        this.password = '';
      },
    });
  }
}
