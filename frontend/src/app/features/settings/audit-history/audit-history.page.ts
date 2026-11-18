import { DatePipe } from '@angular/common';
import { HttpParams } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/api.service';
import { InputComponent } from '../../../shared/components/input/input.component';
import { PageChange, PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { SearchableSelectDirective } from '../../../shared/directives/searchable-select.directive';
import { errorMessage } from '../../../shared/utils/page-utils';

interface AuditRecord {
  id: number;
  action: string;
  entityType: string;
  entityId: string | null;
  actorUserId: number | null;
  actorName: string;
  detailsJson: string;
  createdAt: string;
}

@Component({
  selector: 'app-audit-history-page',
  imports: [DatePipe, FormsModule, InputComponent, PaginationComponent, SearchableSelectDirective],
  templateUrl: './audit-history.page.html',
  styleUrl: './audit-history.page.css',
})
export class AuditHistoryPageComponent implements OnInit {
  readonly actions = [
    { label: 'All actions', value: '' },
    { label: 'Created', value: 'Created' },
    { label: 'Updated', value: 'Updated' },
    { label: 'Deleted', value: 'Deleted' },
    { label: 'Posted', value: 'Posted' },
    { label: 'Status changed', value: 'StatusChanged' },
    { label: 'Profile updated', value: 'ProfileUpdated' },
    { label: 'Authentication succeeded', value: 'AuthenticationSucceeded' },
    { label: 'Authentication failed', value: 'AuthenticationFailed' },
    { label: 'Password reset', value: 'PasswordReset' },
    { label: 'Password changed', value: 'PasswordChanged' },
  ];
  records: AuditRecord[] = [];
  action = '';
  entity = '';
  from = '';
  to = '';
  page = 1;
  pageSize = 25;
  totalCount = 0;
  totalPages = 0;
  loading = false;
  error = '';

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    let params = new HttpParams().set('page', this.page).set('pageSize', this.pageSize);
    if (this.action) params = params.set('action', this.action);
    if (this.entity.trim()) params = params.set('entity', this.entity.trim());
    if (this.from) params = params.set('from', new Date(`${this.from}T00:00:00.000Z`).toISOString());
    if (this.to) params = params.set('to', new Date(`${this.to}T23:59:59.999Z`).toISOString());

    this.api.get<PagedResult<AuditRecord>>('/api/v1/audit', false, params).subscribe({
      next: result => {
        this.records = result.items;
        this.page = result.page;
        this.pageSize = result.pageSize;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.loading = false;
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load audit history.');
        this.loading = false;
      },
    });
  }

  applyFilters(): void {
    this.page = 1;
    this.load();
  }

  onPageChange(event: PageChange): void {
    this.page = event.page;
    this.pageSize = event.pageSize;
    this.load();
  }

  formatDetails(value: string): string {
    try {
      return JSON.stringify(JSON.parse(value), null, 2);
    } catch {
      return value;
    }
  }
}
