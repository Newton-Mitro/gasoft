import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.accessToken();
  return next(token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request).pipe(
      catchError(error => {
        if (error.status === 401 && token) {
          auth.logout();
          void router.navigate(['/login'], { queryParams: { reason: 'session-expired' } });
        }
        return throwError(() => error);
      }),
    );
};
