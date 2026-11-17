import { HttpErrorResponse } from '@angular/common/http';
import { errorMessage, registrationError } from './page-utils';

describe('errorMessage', () => {
  it('uses the API problem detail when available', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { detail: 'The date range is invalid.' },
    });

    expect(errorMessage(error, 'Fallback')).toBe('The date range is invalid.');
  });

  it('keeps validation errors focused on their fields instead of repeating them in an error message', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { errors: { email: ['Enter a valid email.'], password: ['Password is required.'] } },
    });

    expect(errorMessage(error, 'Fallback')).toBe('');
  });

  it('does not turn an empty validation problem into a toast fallback', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { title: 'One or more validation errors occurred.', errors: {} },
    });

    expect(errorMessage(error, 'Fallback')).toBe('');
  });

  it('reports an unavailable API clearly', () => {
    const error = new HttpErrorResponse({ status: 0 });

    expect(errorMessage(error, 'Fallback')).toBe('Could not connect to the API. Is the server running?');
  });

  it('explains registration conflicts without exposing which unique field matched', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: { error: 'An account with that email address already exists.' },
    });

    expect(registrationError(error)).toEqual({
      message: 'We couldn’t create this workspace because some details are already in use. Try a different workspace URL, company code, or email.',
      showSignIn: true,
    });
  });

  it('uses the same actionable message for a workspace URL conflict', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: { error: 'That workspace address is already in use.' },
    });

    expect(registrationError(error)).toEqual({
      message: 'We couldn’t create this workspace because some details are already in use. Try a different workspace URL, company code, or email.',
      showSignIn: true,
    });
  });

  it('keeps registration validation errors focused on their fields', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { errors: { slug: ['Use lowercase letters, numbers, and hyphens.'] } },
    });

    expect(registrationError(error)).toEqual({
      message: '',
      showSignIn: false,
    });
  });
});
