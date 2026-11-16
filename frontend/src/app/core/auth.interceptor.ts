import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError, timeout } from 'rxjs';
import { AuthService } from './auth.service';
import { OptionDataCacheService } from '../shared/data/option-data-cache.service';

const DEFAULT_REQUEST_TIMEOUT_MS = 30_000;

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const optionCache = inject(OptionDataCacheService);
  const router = inject(Router);
  const token = auth.accessToken();
  if (request.method !== 'GET' && request.method !== 'HEAD') optionCache.clear();
  return next(token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request).pipe(
      timeout({ first: DEFAULT_REQUEST_TIMEOUT_MS, each: DEFAULT_REQUEST_TIMEOUT_MS }),
      catchError(error => {
        if (error.status === 401 && token) {
          auth.logout();
          void router.navigate(['/login'], { queryParams: { reason: 'session-expired' } });
        }
        return throwError(() => error);
      }),
    );
};
