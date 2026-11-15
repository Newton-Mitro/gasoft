import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const token = auth.accessToken();
  if (token && !isTokenExpired(token) && auth.companyId()) return true;
  if (token) auth.logout();
  return inject(Router).createUrlTree(['/login']);
};

function isTokenExpired(token: string): boolean {
  try {
    const payload = token.split('.')[1];
    if (!payload) return true;
    const claims = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/'))) as { exp?: unknown };
    return typeof claims.exp !== 'number' || claims.exp * 1000 <= Date.now();
  } catch {
    return true;
  }
}
