import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { FormValidationErrors, FormValidationService } from '../shared/services/form-validation.service';

export const validationInterceptor: HttpInterceptorFn = (request, next) => {
  const formValidation = inject(FormValidationService);
  if (request.method !== 'GET' && request.method !== 'HEAD') {
    formValidation.clear();
  }

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 400) {
        const errors = validationErrorsFrom(error.error);
        if (errors) formValidation.setErrors(errors);
      }
      return throwError(() => error);
    }),
  );
};

function validationErrorsFrom(body: unknown): FormValidationErrors | null {
  if (!body || typeof body !== 'object' || !('errors' in body)) return null;
  const rawErrors = body.errors;
  if (!rawErrors || typeof rawErrors !== 'object') return null;

  const errors: FormValidationErrors = {};
  for (const [field, rawMessages] of Object.entries(rawErrors)) {
    if (Array.isArray(rawMessages)) {
      const messages = rawMessages.filter((message): message is string => typeof message === 'string');
      if (messages.length) errors[field] = messages;
    } else if (typeof rawMessages === 'string' && rawMessages.trim()) {
      errors[field] = [rawMessages];
    }
  }
  return Object.keys(errors).length ? errors : null;
}
