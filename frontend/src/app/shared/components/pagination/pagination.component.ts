import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

export interface PageChange {
  page: number;
  pageSize: number;
}

@Component({
  selector: 'app-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (totalCount > 0) {
      <nav class="pagination" aria-label="Pagination">
        <span class="pagination-summary">
          Showing {{ firstItem }}–{{ lastItem }} of {{ totalCount }}
        </span>
        <div class="pagination-controls">
          <label>
            <span>Rows</span>
            <select [value]="pageSize" (change)="changePageSize($event)">
              @for (size of pageSizes; track size) {
                <option [value]="size">{{ size }}</option>
              }
            </select>
          </label>
          <button type="button" class="button secondary" [disabled]="page <= 1" (click)="goTo(page - 1)">
            Previous
          </button>
          <span class="pagination-page">Page {{ page }} of {{ totalPages }}</span>
          <button
            type="button"
            class="button secondary"
            [disabled]="page >= totalPages"
            (click)="goTo(page + 1)"
          >
            Next
          </button>
        </div>
      </nav>
    }
  `,
  styles: `
    .pagination {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      flex-wrap: wrap;
      padding: 1rem 1.25rem;
      border-top: 1px solid var(--border-color, #e5e7eb);
      color: var(--text-muted, #64748b);
      font-size: 0.875rem;
    }

    .pagination-controls,
    .pagination-controls label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .pagination-controls select {
      min-height: 2.25rem;
      border: 1px solid var(--border-color, #cbd5e1);
      border-radius: 0.375rem;
      padding: 0 0.5rem;
      background: var(--surface-color, white);
      color: inherit;
    }

    .pagination-page {
      white-space: nowrap;
    }
  `,
})
export class PaginationComponent {
  @Input() page = 1;
  @Input() pageSize = 25;
  @Input() totalCount = 0;
  @Input() totalPages = 0;
  @Output() readonly pageChange = new EventEmitter<PageChange>();

  readonly pageSizes = [10, 25, 50, 100];

  get firstItem(): number {
    return (this.page - 1) * this.pageSize + 1;
  }

  get lastItem(): number {
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  goTo(page: number): void {
    if (page >= 1 && page <= this.totalPages && page !== this.page) {
      this.pageChange.emit({ page, pageSize: this.pageSize });
    }
  }

  changePageSize(event: Event): void {
    const pageSize = Number((event.target as HTMLSelectElement).value);
    if (this.pageSizes.includes(pageSize) && pageSize !== this.pageSize) {
      this.pageChange.emit({ page: 1, pageSize });
    }
  }
}
