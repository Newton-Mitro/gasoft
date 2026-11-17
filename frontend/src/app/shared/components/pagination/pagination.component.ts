import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { SearchableSelectDirective } from '../../directives/searchable-select.directive';

export interface PageChange {
  page: number;
  pageSize: number;
}

export type PaginationItem = number | 'ellipsis';

@Component({
  selector: 'app-pagination',
  imports: [SearchableSelectDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (totalCount > 0) {
      <nav class="pagination" aria-label="Pagination">
        <div class="pagination-summary">
          <span class="summary-label">Showing</span>
          <strong>{{ firstItem }}–{{ lastItem }}</strong>
          <span>of {{ totalCount }}</span>
        </div>
        <div class="pagination-controls">
          <label class="page-size">
            <span>Rows</span>
            <select appSearchableSelect [value]="pageSize" [disabled]="loading" (change)="changePageSize($event)">
              @for (size of pageSizes; track size) {
                <option [value]="size">{{ size }}</option>
              }
            </select>
          </label>
          <div class="pagination-navigation" aria-label="Page navigation">
            <button type="button" class="page-nav" aria-label="First page" [disabled]="loading || page <= 1" (click)="goTo(1)">
              <span aria-hidden="true">«</span><span class="nav-label">First</span>
            </button>
            <button type="button" class="page-nav" aria-label="Previous page" [disabled]="loading || page <= 1" (click)="goTo(page - 1)">
              <span aria-hidden="true">‹</span><span class="nav-label">Previous</span>
            </button>
            <div class="pagination-pages" aria-label="Page numbers">
              @for (item of paginationItems; track $index) {
                @if (item === 'ellipsis') {
                  <span class="pagination-ellipsis" aria-hidden="true">…</span>
                } @else {
                  <button
                    type="button"
                    class="pagination-number"
                    [class.current]="item === page"
                    [class.edge]="item === 1 || item === totalPages"
                    [class.nearby]="item === page - 1 || item === page + 1"
                    [attr.aria-current]="item === page ? 'page' : null"
                    [attr.aria-label]="'Page ' + item"
                    [disabled]="loading || item === page"
                    (click)="goTo(item)"
                  >
                    {{ item }}
                  </button>
                }
              }
            </div>
            <button type="button" class="page-nav" aria-label="Next page" [disabled]="loading || page >= totalPages" (click)="goTo(page + 1)">
              <span class="nav-label">Next</span><i class="fa-solid fa-chevron-right" aria-hidden="true"></i>
            </button>
            <button type="button" class="page-nav" aria-label="Last page" [disabled]="loading || page >= totalPages" (click)="goTo(totalPages)">
              <span class="nav-label">Last</span><span aria-hidden="true">»</span>
            </button>
          </div>
        </div>
      </nav>
    }
  `,
  styles: `
    .pagination {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1.25rem;
      flex-wrap: wrap;
      padding: 0.75rem 1.25rem;
      border-top: 1px solid var(--border);
      background: color-mix(in srgb, var(--surface) 92%, var(--accent) 8%);
      color: var(--text-muted);
      font-size: 0.75rem;
    }

    .pagination-summary {
      display: inline-flex;
      align-items: baseline;
      gap: 0.35rem;
      white-space: nowrap;
    }

    .pagination-summary strong {
      color: var(--text-strong);
      font-variant-numeric: tabular-nums;
      font-weight: 700;
    }

    .summary-label,
    .page-size > span {
      color: var(--text-muted);
    }

    .pagination-controls,
    .page-size,
    .pagination-navigation {
      display: flex;
      align-items: center;
    }

    .pagination-controls {
      gap: 0.85rem;
    }

    .page-size {
      gap: 0.4rem;
      color: var(--text-muted);
      font-size: 0.7rem;
      font-weight: 600;
      white-space: nowrap;
    }

    .pagination-controls select {
      min-height: 2rem;
      border: 1px solid var(--border);
      border-radius: 0.55rem;
      padding: 0 1.45rem 0 0.55rem;
      background: var(--surface);
      color: var(--text-strong);
      font: inherit;
      font-weight: 650;
      cursor: pointer;
      outline: none;
      transition: border-color .15s ease, box-shadow .15s ease;
    }

    .pagination-controls select:focus-visible {
      border-color: var(--focus-color);
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--focus-color) 18%, transparent);
    }

    .pagination-navigation {
      gap: 0.25rem;
      padding: 0.2rem;
      border: 1px solid var(--border);
      border-radius: 0.7rem;
      background: var(--surface);
      box-shadow: 0 1px 2px color-mix(in srgb, var(--text-strong) 6%, transparent);
    }

    .pagination-pages {
      display: flex;
      align-items: center;
      gap: 0.1rem;
      margin: 0 0.15rem;
    }

    .page-nav,
    .pagination-number {
      display: inline-flex;
      min-width: 2rem;
      height: 2rem;
      align-items: center;
      justify-content: center;
      gap: 0.28rem;
      padding: 0 0.5rem;
      border: 1px solid transparent;
      border-radius: 0.48rem;
      background: transparent;
      color: var(--text-muted);
      font: inherit;
      font-size: 0.72rem;
      font-weight: 650;
      white-space: nowrap;
      cursor: pointer;
      transition: color .15s ease, background-color .15s ease, border-color .15s ease;
    }

    .page-nav:not(:disabled):hover,
    .pagination-number:not(:disabled):hover {
      border-color: color-mix(in srgb, var(--accent) 18%, var(--border));
      background: var(--accent-soft);
      color: var(--accent-text);
    }

    .page-nav:disabled {
      color: color-mix(in srgb, var(--text-muted) 50%, var(--surface));
      cursor: not-allowed;
    }

    .pagination-number.current {
      border-color: var(--accent);
      background: var(--accent);
      color: #fff;
      box-shadow: 0 2px 5px color-mix(in srgb, var(--accent) 25%, transparent);
    }

    .pagination-ellipsis {
      min-width: 1.15rem;
      color: var(--text-muted);
      text-align: center;
    }

    .page-nav:focus-visible,
    .pagination-number:focus-visible {
      outline: 2px solid var(--focus-color);
      outline-offset: 2px;
    }

    @media (max-width: 760px) {
      .pagination {
        align-items: flex-start;
        flex-direction: column;
        gap: 0.65rem;
        padding: 0.75rem 0.9rem;
      }

      .pagination-controls {
        width: 100%;
        justify-content: space-between;
      }
    }

    @media (max-width: 540px) {
      .pagination-controls {
        align-items: flex-start;
        flex-direction: column;
        gap: 0.5rem;
      }

      .pagination-navigation {
        max-width: 100%;
        min-width: 0;
        align-self: stretch;
        justify-content: space-between;
        overflow: hidden;
      }

      .page-nav,
      .pagination-number {
        min-width: 1.8rem;
        flex: 0 0 auto;
        padding-inline: 0.35rem;
      }

      .pagination-pages {
        min-width: 0;
        gap: 0;
        margin-inline: 0;
      }

      .pagination-number:not(.current):not(.edge):not(.nearby),
      .pagination-ellipsis {
        display: none;
      }

      .nav-label {
        display: none;
      }
    }

    @media (max-width: 380px) {
      .pagination {
        padding-inline: 0.65rem;
      }

      .pagination-number:not(.current):not(.edge) {
        display: none;
      }

      .pagination-controls {
        gap: 0.4rem;
      }

      .page-nav,
      .pagination-number {
        min-width: 1.65rem;
        padding-inline: 0.25rem;
      }
    }
  `,
})
export class PaginationComponent {
  @Input() page = 1;
  @Input() pageSize = 25;
  @Input() totalCount = 0;
  @Input() totalPages = 0;
  @Input() loading = false;
  @Output() readonly pageChange = new EventEmitter<PageChange>();

  readonly pageSizes = [10, 25, 50, 100];

  get firstItem(): number {
    return (this.page - 1) * this.pageSize + 1;
  }

  get lastItem(): number {
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  get paginationItems(): PaginationItem[] {
    if (this.totalPages <= 0) return [];

    const pages = new Set<number>();
    for (let page = 1; page <= Math.min(3, this.totalPages); page++) pages.add(page);
    for (let page = Math.max(1, this.page - 1); page <= Math.min(this.totalPages, this.page + 1); page++) {
      pages.add(page);
    }
    for (let page = Math.max(1, this.totalPages - 1); page <= this.totalPages; page++) pages.add(page);

    const sortedPages = [...pages].sort((a, b) => a - b);
    const items: PaginationItem[] = [];
    for (const [index, currentPage] of sortedPages.entries()) {
      const previousPage = sortedPages[index - 1];
      if (previousPage !== undefined) {
        const gap = currentPage - previousPage;
        if (gap === 2) items.push(previousPage + 1);
        else if (gap > 2) items.push('ellipsis');
      }
      items.push(currentPage);
    }
    return items;
  }

  goTo(page: number): void {
    if (!this.loading && page >= 1 && page <= this.totalPages && page !== this.page) {
      this.pageChange.emit({ page, pageSize: this.pageSize });
    }
  }

  changePageSize(event: Event): void {
    const pageSize = Number((event.target as HTMLSelectElement).value);
    if (!this.loading && this.pageSizes.includes(pageSize) && pageSize !== this.pageSize) {
      this.pageChange.emit({ page: 1, pageSize });
    }
  }
}
