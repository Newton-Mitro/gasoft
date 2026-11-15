import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { Company, UpdateCompanyPayload } from './company.model';

@Injectable({ providedIn: 'root' })
export class CompanyService {
  constructor(private readonly api: ApiService) {}

  getById(id: number): Observable<Company> {
    return this.api.get<unknown>(`/api/v1/companies/${id}`, false).pipe(map(toCompany));
  }

  update(id: number, payload: UpdateCompanyPayload): Observable<Company> {
    return this.api.put<unknown>(`/api/v1/companies/${id}`, payload, false).pipe(map(toCompany));
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
    typeof row['functionalCurrencyCode'] === 'string';
}
