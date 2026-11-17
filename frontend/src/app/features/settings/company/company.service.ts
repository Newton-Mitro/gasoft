import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { Company, CreateCompanyPayload, UpdateCompanyPayload } from './company.model';

@Injectable({ providedIn: 'root' })
export class CompanyService {
  constructor(private readonly api: ApiService) {}

  getById(id: number): Observable<Company> {
    return this.api.get<unknown>(`/api/v1/companies/${id}`, false).pipe(map(toCompany));
  }

  exportData(companyId: number): Observable<Blob> {
    return this.api.getBlob(`/api/v1/companies/${companyId}/export`, false);
  }

  create(payload: CreateCompanyPayload): Observable<Company> {
    return this.api.post<unknown>('/api/v1/tenant/companies', payload, false).pipe(map(toCompany));
  }

  update(id: number, payload: UpdateCompanyPayload): Observable<Company> {
    return this.api.put<unknown>(`/api/v1/companies/${id}`, payload, false).pipe(map(toCompany));
  }

  uploadLogo(id: number, image: File): Observable<Company> {
    const formData = new FormData();
    formData.append('image', image);
    return this.api.post<unknown>(`/api/v1/companies/${id}/logo`, formData, false).pipe(map(toCompany));
  }
}

function toCompany(value: unknown): Company {
  if (!isCompany(value)) throw new Error('The company response contains an invalid record.');
  return value;
}

function isCompany(value: unknown): value is Company {
  if (!value || typeof value !== 'object') return false;
  const row = value as Record<string, unknown>;
  return typeof row['id'] === 'number' && Number.isSafeInteger(row['id']) &&
    typeof row['code'] === 'string' &&
    typeof row['legalName'] === 'string' &&
    typeof row['functionalCurrencyCode'] === 'string' &&
    isNullableString(row['phone']) &&
    isNullableString(row['email']) &&
    isNullableString(row['website']) &&
    isNullableString(row['address']) &&
    isNullableString(row['cityRegion']) &&
    isNullableString(row['postalCode']) &&
    isNullableString(row['country']) &&
    isNullableString(row['taxId']) &&
    isNullableString(row['logoUrl']);
}

function isNullableString(value: unknown): value is string | null {
  return value === null || typeof value === 'string';
}
