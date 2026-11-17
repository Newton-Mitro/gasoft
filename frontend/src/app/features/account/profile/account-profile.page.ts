import { Component, OnDestroy, OnInit, signal, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountProfile, AuthService } from '../../../core/auth.service';
import { errorMessage } from '../../../shared/utils/page-utils';
import { AuthenticatedImageComponent } from '../../../shared/components/authenticated-image/authenticated-image.component';
import { NotificationService } from '../../../shared/services/notification.service';
import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';

@Component({
  selector: 'app-account-profile-page',
  imports: [FormsModule, AuthenticatedImageComponent, FieldValidationDirective],
  templateUrl: './account-profile.page.html',
  styleUrl: './account-profile.page.css',
})
export class AccountProfilePage implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  readonly profile = signal<AccountProfile | null>(null);
  readonly loading = signal(true);
  readonly profileSaving = signal(false);
  readonly passwordSaving = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly profileImagePreview = signal<string | null>(null);
  readonly imageUploading = signal(false);
  selectedImage: File | null = null;
  private imageObjectUrl: string | null = null;
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
        this.error.set('');
        this.notifications.error(errorMessage(error, 'Could not load your profile.'));
        this.loading.set(false);
      },
    });
  }

  ngOnDestroy(): void {
    this.revokeImagePreview();
  }

  get profileImageUrl(): string | null {
    return this.profileImagePreview() ?? this.profile()?.profileImageUrl ?? null;
  }

  onImageSelected(event: Event): void {
    this.error.set('');
    const input = event.currentTarget as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (!file) return;
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) {
      this.error.set('Choose a JPEG, PNG, or WebP image no larger than 5 MB.');
      input.value = '';
      return;
    }

    input.value = '';
    this.revokeImagePreview();
    this.selectedImage = file;
    this.imageObjectUrl = URL.createObjectURL(file);
    this.profileImagePreview.set(this.imageObjectUrl);
  }

  uploadProfileImage(): void {
    if (!this.selectedImage) return;
    this.error.set('');
    this.success.set('');
    this.imageUploading.set(true);
    this.auth.uploadProfileImage(this.selectedImage).subscribe({
      next: profile => {
        this.profile.set(profile);
        this.selectedImage = null;
        this.revokeImagePreview();
        this.notifications.success('Your profile image has been updated.');
        this.success.set('');
        this.imageUploading.set(false);
      },
      error: error => {
        this.error.set('');
        this.notifications.error(errorMessage(error, 'Could not upload your profile image.'));
        this.imageUploading.set(false);
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
        this.notifications.success('Your profile has been updated.');
        this.success.set('');
        this.profileSaving.set(false);
      },
      error: error => {
        this.error.set('');
        this.notifications.error(errorMessage(error, 'Could not update your profile.'));
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
        this.notifications.success('Your password has been changed.');
        this.success.set('');
        this.passwordSaving.set(false);
      },
      error: error => {
        this.error.set('');
        this.notifications.error(errorMessage(error, 'Could not change your password.'));
        this.passwordSaving.set(false);
      },
    });
  }

  private revokeImagePreview(): void {
    if (this.imageObjectUrl) URL.revokeObjectURL(this.imageObjectUrl);
    this.imageObjectUrl = null;
    this.profileImagePreview.set(null);
  }
}
