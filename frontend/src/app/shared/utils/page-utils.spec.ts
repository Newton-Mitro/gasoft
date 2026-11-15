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

  it('joins validation errors returned by the API', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { errors: { email: ['Enter a valid email.'], password: ['Password is required.'] } },
    });

    expect(errorMessage(error, 'Fallback')).toBe('Enter a valid email. Password is required.');
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

  it('labels registration validation messages by field', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { errors: { slug: ['Use lowercase letters, numbers, and hyphens.'] } },
    });

    expect(registrationError(error)).toEqual({
      message: 'Workspace URL: Use lowercase letters, numbers, and hyphens.',
      showSignIn: false,
    });
  });
});
