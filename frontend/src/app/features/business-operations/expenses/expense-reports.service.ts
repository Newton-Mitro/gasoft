import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { ExpenseReport, ExpenseReportPayload } from './expense-report.model';

const ENDPOINT = 'expense-reports';

@Injectable({ providedIn: 'root' })
export class ExpenseReportsService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<ExpenseReport[]> {
    return this.api.get<ExpenseReport[]>(ENDPOINT);
  }

  create(payload: ExpenseReportPayload): Observable<ExpenseReport> {
    return this.api.post<ExpenseReport>(ENDPOINT, payload);
  }

  update(id: number, payload: ExpenseReportPayload): Observable<ExpenseReport> {
    return this.api.put<ExpenseReport>(`${ENDPOINT}/${id}`, payload);
  }

  submit(id: number): Observable<ExpenseReport> {
    return this.api.post<ExpenseReport>(`${ENDPOINT}/${id}/submit`, {});
  }

  approve(id: number, note: string | null): Observable<ExpenseReport> {
    return this.api.post<ExpenseReport>(`${ENDPOINT}/${id}/approve`, { note });
  }

  reject(id: number, note: string): Observable<ExpenseReport> {
    return this.api.post<ExpenseReport>(`${ENDPOINT}/${id}/reject`, { note });
  }

  post(
    id: number,
    journalId: number,
    accountingPeriodId: number,
    employeePayableAccountId: number,
  ): Observable<ExpenseReport> {
    return this.api.post<ExpenseReport>(`${ENDPOINT}/${id}/post`, {
      journalId,
      accountingPeriodId,
      employeePayableAccountId,
    });
  }

  reimburse(
    id: number,
    journalId: number,
    accountingPeriodId: number,
    cashAccountId: number,
  ): Observable<ExpenseReport> {
    return this.api.post<ExpenseReport>(`${ENDPOINT}/${id}/reimburse`, {
      journalId,
      accountingPeriodId,
      cashAccountId,
    });
  }

  uploadReceipt(reportId: number, lineId: number, file: File): Observable<ExpenseReport> {
    const formData = new FormData();
    formData.append('receipt', file);
    return this.api.post<ExpenseReport>(
      `${ENDPOINT}/${reportId}/lines/${lineId}/receipt`,
      formData,
    );
  }

  getReceipt(receiptUrl: string): Observable<Blob> {
    return this.api.getBlob(receiptUrl, false);
  }
}
