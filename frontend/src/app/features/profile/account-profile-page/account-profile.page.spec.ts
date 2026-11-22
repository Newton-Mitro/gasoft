import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { FormValidationService } from '../../../shared/services/form-validation.service';
import { AccountProfilePage } from './account-profile.page';

describe('AccountProfilePage', () => {
  const createPage = (auth: AuthService, notifications = { success: vi.fn(), error: vi.fn() }) => {
    TestBed.configureTestingModule({
      providers: [{ provide: NotificationService, useValue: notifications }],
    });
    const page = TestBed.runInInjectionContext(() => new AccountProfilePage(auth));
    return { page, notifications };
  };

  it('does not send a password change when confirmation does not match', () => {
    let changeRequested = false;
    const auth = {
      changePassword: () => {
        changeRequested = true;
        return of(undefined);
      },
    } as unknown as AuthService;
    const { page, notifications } = createPage(auth);
    page.currentPassword = 'current-password';
    page.newPassword = 'New-password-123!';
    page.confirmPassword = 'different-password';

    page.updatePassword();

    expect(changeRequested).toBe(false);
    expect(page.error()).toBe('');
    expect(TestBed.inject(FormValidationService).messagesFor('confirmPassword')).toEqual([
      'The new passwords do not match.',
    ]);
    expect(notifications.error).not.toHaveBeenCalled();
  });

  it('requires all password fields before submitting', () => {
    const auth = {
      changePassword: vi.fn(() => of(undefined)),
    } as unknown as AuthService;
    const { page } = createPage(auth);

    page.updatePassword();

    const validation = TestBed.inject(FormValidationService);
    expect(auth.changePassword).not.toHaveBeenCalled();
    expect(validation.messagesFor('currentPassword')).toEqual(['Enter your current password.']);
    expect(validation.messagesFor('newPassword')).toEqual(['Enter a new password.']);
    expect(validation.messagesFor('confirmPassword')).toEqual(['Confirm your new password.']);
  });

  it('does not update the profile when the display name is blank', () => {
    const auth = {
      updateProfile: vi.fn(() => of({ displayName: 'Newton Mitro' })),
    } as unknown as AuthService;
    const { page } = createPage(auth);
    page.displayName = '   ';

    page.updateProfile();

    expect(auth.updateProfile).not.toHaveBeenCalled();
    expect(TestBed.inject(FormValidationService).messagesFor('displayName')).toEqual([
      'Enter your display name.',
    ]);
  });

  it('does not send a password that fails the password policy', () => {
    const auth = {
      changePassword: vi.fn(() => of(undefined)),
    } as unknown as AuthService;
    const { page } = createPage(auth);
    page.currentPassword = 'current-password';
    page.newPassword = 'abcdefghijk';
    page.confirmPassword = page.newPassword;

    page.updatePassword();

    expect(auth.changePassword).not.toHaveBeenCalled();
    expect(TestBed.inject(FormValidationService).messagesFor('newPassword')).toEqual([
      'Use at least 12 characters.',
      'Include at least one number.',
      'Include at least one symbol.',
    ]);
  });

  it('clears password fields after a successful change', () => {
    let changeRequested = false;
    const auth = {
      changePassword: () => {
        changeRequested = true;
        return of(undefined);
      },
    } as unknown as AuthService;
    const { page, notifications } = createPage(auth);
    page.currentPassword = 'current-password';
    page.newPassword = 'New-password-123!';
    page.confirmPassword = 'New-password-123!';

    page.updatePassword();

    expect(changeRequested).toBe(true);
    expect(page.currentPassword).toBe('');
    expect(page.newPassword).toBe('');
    expect(page.confirmPassword).toBe('');
    expect(page.success()).toBe('');
    expect(notifications.success).toHaveBeenCalledWith('Your password has been changed.');
  });
});
