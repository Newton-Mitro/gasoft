import { of } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { AccountProfilePage } from './account-profile.page';

describe('AccountProfilePage', () => {
  it('does not send a password change when confirmation does not match', () => {
    let changeRequested = false;
    const auth = {
      changePassword: () => {
        changeRequested = true;
        return of(undefined);
      },
    } as unknown as AuthService;
    const page = new AccountProfilePage(auth);
    page.currentPassword = 'current-password';
    page.newPassword = 'New-password-123!';
    page.confirmPassword = 'different-password';

    page.updatePassword();

    expect(changeRequested).toBe(false);
    expect(page.error()).toBe('The new passwords do not match.');
  });

  it('clears password fields after a successful change', () => {
    let changeRequested = false;
    const auth = {
      changePassword: () => {
        changeRequested = true;
        return of(undefined);
      },
    } as unknown as AuthService;
    const page = new AccountProfilePage(auth);
    page.currentPassword = 'current-password';
    page.newPassword = 'New-password-123!';
    page.confirmPassword = 'New-password-123!';

    page.updatePassword();

    expect(changeRequested).toBe(true);
    expect(page.currentPassword).toBe('');
    expect(page.newPassword).toBe('');
    expect(page.confirmPassword).toBe('');
    expect(page.success()).toBe('Your password has been changed.');
  });
});
