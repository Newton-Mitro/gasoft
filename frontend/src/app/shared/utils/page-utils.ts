import { HttpErrorResponse } from '@angular/common/http';

export function errorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) return fallback;
  const body: unknown = error.error;
  if (typeof body === 'string' && body.trim()) return body;
  if (body && typeof body === 'object') {
    const details = body as { detail?: unknown; error?: unknown; title?: unknown; errors?: unknown };
    if (isValidationProblem(error, details.errors)) return '';
    if (typeof details.detail === 'string') return details.detail;
    if (typeof details.error === 'string') return details.error;
    if (typeof details.title === 'string') return details.title;
  }
  return error.status === 0 ? 'Could not connect to the API. Is the server running?' : fallback;
}

function isValidationProblem(error: HttpErrorResponse, errors: unknown): boolean {
  return (error.status === 400 || error.status === 422) &&
    !!errors && typeof errors === 'object' && !Array.isArray(errors);
}

export function registrationError(error: unknown): { message: string; showSignIn: boolean } {
  if (!(error instanceof HttpErrorResponse)) {
    return { message: 'We could not create your workspace. Please try again.', showSignIn: false };
  }

  if (error.status === 409) {
    return {
      message: 'We couldn’t create this workspace because some details are already in use. Try a different workspace URL, company code, or email.',
      showSignIn: true,
    };
  }

  if (error.status === 429) {
    return { message: 'Too many registration attempts. Please wait a moment and try again.', showSignIn: false };
  }
  if (error.status >= 500) {
    return { message: 'We could not create your workspace right now. Please try again later.', showSignIn: false };
  }

  if (error.error && typeof error.error === 'object' &&
    isValidationProblem(error, (error.error as { errors?: unknown }).errors)) {
    return { message: '', showSignIn: false };
  }

  return {
    message: errorMessage(error, 'Please review your details and try again.'),
    showSignIn: false,
  };
}
