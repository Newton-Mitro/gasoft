import { HttpErrorResponse } from '@angular/common/http';

export function errorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) return fallback;
  const body: unknown = error.error;
  if (typeof body === 'string' && body.trim()) return body;
  if (body && typeof body === 'object') {
    const details = body as { detail?: unknown; error?: unknown; title?: unknown; errors?: unknown };
    if (typeof details.detail === 'string') return details.detail;
    if (typeof details.error === 'string') return details.error;
    if (details.errors && typeof details.errors === 'object') {
      const messages = Object.values(details.errors).flatMap(value =>
        Array.isArray(value) ? value.filter((item): item is string => typeof item === 'string') : []);
      if (messages.length) return messages.join(' ');
    }
    if (typeof details.title === 'string') return details.title;
  }
  return error.status === 0 ? 'Could not connect to the API. Is the server running?' : fallback;
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

  if (error.status === 400 && error.error && typeof error.error === 'object' && 'errors' in error.error) {
    const validationErrors = (error.error as { errors?: unknown }).errors;
    if (validationErrors && typeof validationErrors === 'object') {
      const fieldNames: Record<string, string> = {
        tenantName: 'Workspace name',
        slug: 'Workspace URL',
        companyCode: 'Company code',
        legalName: 'Legal name',
        functionalCurrencyCode: 'Functional currency',
        ownerDisplayName: 'Your name',
        email: 'Email address',
        password: 'Password',
      };
      const messages = Object.entries(validationErrors).flatMap(([field, value]) => {
        if (!Array.isArray(value)) return [];
        return value
          .filter((message): message is string => typeof message === 'string')
          .map(message => `${fieldNames[field] ?? field}: ${message}`);
      });
      if (messages.length) return { message: messages.join(' '), showSignIn: false };
    }
  }

  return {
    message: errorMessage(error, 'Please review your details and try again.'),
    showSignIn: false,
  };
}
