import { DatePipe } from '@angular/common';
import { HttpParams } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../../core/api.service';
import { InputComponent } from '../../../../shared/components/input/input.component';
import { PageChange, PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { PagedResult } from '../../../../shared/models/paged-result.model';
import { SearchableSelectDirective } from '../../../../shared/directives/searchable-select.directive';
import { errorMessage } from '../../../../shared/utils/page-utils';

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

interface AuditActionDefinition {
  action: string;
  category: string;
  name: string;
}

interface AuditHistoryResponse extends PagedResult<AuditRecord> {
  actions: AuditActionDefinition[];
}

@Component({
  selector: 'app-audit-history-page',
  imports: [DatePipe, FormsModule, InputComponent, PaginationComponent, SearchableSelectDirective],
  templateUrl: './audit-history.page.html',
  styleUrl: './audit-history.page.css',
})
export class AuditHistoryPageComponent implements OnInit {
  actions: { label: string; value: string }[] = [{ label: 'All actions', value: '' }];
  private actionDefinitions = new Map<string, AuditActionDefinition>();
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

  constructor(
    private readonly api: ApiService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

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

    this.api.get<AuditHistoryResponse>('/api/v1/audit', false, params).subscribe({
      next: result => {
        this.records = result.items;
        this.actionDefinitions = new Map(result.actions.map(item => [item.action, item]));
        this.actions = [
          { label: 'All actions', value: '' },
          ...result.actions.map(item => ({
            label: `${item.category} · ${item.name}`,
            value: item.action,
          })),
        ];
        this.page = result.page;
        this.pageSize = result.pageSize;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load audit history.');
        this.loading = false;
        this.changeDetector.markForCheck();
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

  formatAction(action: string): string {
    return this.actionDefinitions.get(action)?.name ??
      action.replace(/-/g, ' ')
        .replace(/([a-z])([A-Z])/g, '$1 $2')
        .replace(/^./, character => character.toUpperCase());
  }

  isWorkflowAction(action: string): boolean {
    const category = this.actionDefinitions.get(action)?.category;
    return !!category &&
      !['General', 'Company', 'Security', 'Administration'].includes(category);
  }

  isPostedAction(action: string): boolean {
    return action === 'Posted' || action.endsWith('-posted');
  }

  isSecurityAction(action: string): boolean {
    return this.actionDefinitions.get(action)?.category === 'Security' ||
      action.startsWith('Authentication') ||
      action.includes('Password');
  }

  formatEntityType(entityType: string): string {
    const labels: Record<string, string> = {
      ChartOfAccount: 'Chart of account',
      FiscalYearStatusChange: 'Fiscal year',
      AccountingPeriodStatusChange: 'Accounting period',
      JournalEntry: 'Journal entry',
      Company: 'Company',
      ApplicationUser: 'User',
    };
    return labels[entityType] ?? entityType
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/^./, character => character.toUpperCase());
  }
}
