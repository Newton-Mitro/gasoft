import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { errorMessage } from '../../../shared/utils/page-utils';
import { PageChange, PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { InventoryBalance } from './inventory-balance.model';
import { InventoryBalanceService } from './inventory-balance.service';

@Component({
  selector: 'app-inventory-balances-page',
  imports: [PaginationComponent],
  templateUrl: './inventory-balances.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class InventoryBalancesPageComponent implements OnInit {
  readonly title = 'Stock balances';
  readonly description = 'Review current quantity and inventory value by item and warehouse.';
  rows: InventoryBalance[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  loading = false;
  error = '';

  constructor(
    private readonly balances: InventoryBalanceService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadRows();
  }

  onPageChange(change: PageChange): void {
    this.page = change.page;
    this.pageSize = change.pageSize;
    this.loadRows();
  }

  private loadRows(): void {
    this.loading = true;
    this.balances.getPage(this.page, this.pageSize).subscribe({
      next: result => {
        this.rows = result.items;
        this.page = result.page;
        this.pageSize = result.pageSize;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load stock balances.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  formatValue(value: number | string): string {
    const amount = Number(value);
    if (!Number.isFinite(amount)) return String(value);
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: 'USD',
      maximumFractionDigits: 2,
    }).format(amount);
  }
}
