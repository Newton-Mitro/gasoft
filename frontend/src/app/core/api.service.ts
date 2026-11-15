import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private readonly http: HttpClient, private readonly auth: AuthService) {}

  companyUrl(path: string): string {
    const companyId = this.auth.companyId();
    if (!companyId) throw new Error('Choose a company before loading company data.');
    const resource = path.startsWith('/') ? path.slice(1) : path;
    return `/api/v1/companies/${companyId}/${resource}`;
  }

  get<T>(path: string, companyScoped = true, params?: HttpParams) {
    return this.http.get<T>(companyScoped ? this.companyUrl(path) : path, { params });
  }

  post<T>(path: string, body: unknown, companyScoped = true) {
    return this.http.post<T>(companyScoped ? this.companyUrl(path) : path, body);
  }

  put<T>(path: string, body: unknown, companyScoped = true) {
    return this.http.put<T>(companyScoped ? this.companyUrl(path) : path, body);
  }
}
