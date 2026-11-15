import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountProfile, AuthService } from '../../../core/auth.service';
import { errorMessage } from '../../../shared/utils/page-utils';

@Component({
  selector: 'app-account-profile-page',
  imports: [FormsModule],
  templateUrl: './account-profile.page.html',
  styleUrl: './account-profile.page.css',
})
export class AccountProfilePage implements OnInit {
  readonly profile = signal<AccountProfile | null>(null);
  readonly loading = signal(true);
  readonly profileSaving = signal(false);
  readonly passwordSaving = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  displayName = '';
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';

  constructor(private readonly auth: AuthService) {}

  ngOnInit(): void {
    this.auth.getProfile().subscribe({
      next: profile => {
        this.profile.set(profile);
        this.displayName = profile.displayName;
        this.loading.set(false);
      },
      error: error => {
        this.error.set(errorMessage(error, 'Could not load your profile.'));
        this.loading.set(false);
      },
    });
  }

  updateProfile(): void {
    this.error.set('');
    this.success.set('');
    this.profileSaving.set(true);
    this.auth.updateProfile(this.displayName.trim()).subscribe({
      next: profile => {
        this.profile.set(profile);
        this.displayName = profile.displayName;
        this.success.set('Your profile has been updated.');
        this.profileSaving.set(false);
      },
      error: error => {
        this.error.set(errorMessage(error, 'Could not update your profile.'));
        this.profileSaving.set(false);
      },
    });
  }

  updatePassword(): void {
    this.error.set('');
    this.success.set('');
    if (this.newPassword !== this.confirmPassword) {
      this.error.set('The new passwords do not match.');
      return;
    }

    this.passwordSaving.set(true);
    this.auth.changePassword(this.currentPassword, this.newPassword).subscribe({
      next: () => {
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
        this.success.set('Your password has been changed.');
        this.passwordSaving.set(false);
      },
      error: error => {
        this.error.set(errorMessage(error, 'Could not change your password.'));
        this.passwordSaving.set(false);
      },
    });
  }
}
