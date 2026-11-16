import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { OptionDataCacheService } from '../shared/data/option-data-cache.service';

interface LoginResponse {
  accessToken: string;
  tenantId: number;
  companyId: number;
  workspaceSlug: string;
}

export interface WorkspaceOption {
  tenantId: number;
  name: string;
}

export interface RegistrationResponse {
  tenantId: number;
  companyId: number;
  accessToken: string;
}

export interface AccountProfile {
  displayName: string;
  email: string;
  profileImageUrl: string | null;
}

interface RegistrationRequest {
  tenantName: string;
  slug: string;
  companyCode: string;
  legalName: string;
  functionalCurrencyCode: string;
  ownerDisplayName: string;
  email: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenKey = 'gasoft.accessToken';
  private readonly companyKey = 'gasoft.companyId';
  private readonly tenantKey = 'gasoft.tenantId';
  private readonly workspaceSlugKey = 'gasoft.workspaceSlug';
  readonly accessToken = signal<string | null>(localStorage.getItem(this.tokenKey));
  readonly companyId = signal<number | null>(this.readId(this.companyKey));
  readonly tenantId = signal<number | null>(this.readId(this.tenantKey));
  readonly workspaceSlug = signal(localStorage.getItem(this.workspaceSlugKey) ?? '');
  readonly profile = signal<AccountProfile | null>(null);
  readonly companyLogoUrl = signal<string | null>(null);

  constructor(
    private readonly http: HttpClient,
    private readonly optionCache: OptionDataCacheService,
  ) {}

  getSignInWorkspaces(email: string, password: string): Observable<WorkspaceOption[]> {
    return this.http.post<WorkspaceOption[]>('/api/v1/auth/workspaces', { email, password });
  }

  login(email: string, password: string, tenantId: number): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/v1/auth/token', { email, password, tenantId }).pipe(
      tap(result => this.save(
        result.accessToken,
        result.tenantId,
        result.companyId,
        result.workspaceSlug,
      )),
    );
  }

  register(request: RegistrationRequest): Observable<RegistrationResponse> {
    return this.http.post<RegistrationResponse>('/api/v1/tenants/register', request).pipe(
      tap(result => this.save(result.accessToken, result.tenantId, result.companyId, request.slug)),
    );
  }

  getProfile(): Observable<AccountProfile> {
    return this.http.get<AccountProfile>('/api/v1/account/profile').pipe(
      tap(profile => this.profile.set(profile)),
    );
  }

  updateProfile(displayName: string): Observable<AccountProfile> {
    return this.http.put<AccountProfile>('/api/v1/account/profile', { displayName }).pipe(
      tap(profile => this.profile.set(profile)),
    );
  }

  uploadProfileImage(image: File): Observable<AccountProfile> {
    const formData = new FormData();
    formData.append('image', image);
    return this.http.post<AccountProfile>('/api/v1/account/profile/image', formData).pipe(
      tap(profile => this.profile.set(profile)),
    );
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>('/api/v1/account/password', { currentPassword, newPassword });
  }

  requestPasswordReset(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>('/api/v1/auth/password/forgot', { email });
  }

  resetPassword(email: string, token: string, newPassword: string): Observable<void> {
    return this.http.post<void>('/api/v1/auth/password/reset', { email, token, newPassword });
  }

  updateCompanyId(companyId: number): void {
    this.optionCache.clear();
    localStorage.setItem(this.companyKey, String(companyId));
    this.companyId.set(companyId);
    this.companyLogoUrl.set(null);
  }

  updateCompanyLogoUrl(logoUrl: string | null): void {
    this.companyLogoUrl.set(logoUrl);
  }

  logout(): void {
    this.optionCache.clear();
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.companyKey);
    localStorage.removeItem(this.tenantKey);
    localStorage.removeItem(this.workspaceSlugKey);
    this.accessToken.set(null);
    this.companyId.set(null);
    this.tenantId.set(null);
    this.workspaceSlug.set('');
    this.profile.set(null);
    this.companyLogoUrl.set(null);
  }

  private save(token: string, tenantId: number, companyId: number, workspaceSlug: string): void {
    this.optionCache.clear();
    localStorage.setItem(this.tokenKey, token);
    localStorage.setItem(this.tenantKey, String(tenantId));
    localStorage.setItem(this.companyKey, String(companyId));
    localStorage.setItem(this.workspaceSlugKey, workspaceSlug);
    this.accessToken.set(token);
    this.tenantId.set(tenantId);
    this.companyId.set(companyId);
    this.workspaceSlug.set(workspaceSlug);
  }

  private readId(key: string): number | null {
    const value = Number(localStorage.getItem(key));
    return Number.isSafeInteger(value) && value > 0 ? value : null;
  }
}
