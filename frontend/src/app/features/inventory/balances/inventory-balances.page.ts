import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { errorMessage } from '../../../shared/utils/page-utils';
import { InventoryBalance } from './inventory-balance.model';
import { InventoryBalanceService } from './inventory-balance.service';

@Component({
  selector: 'app-inventory-balances-page',
  templateUrl: './inventory-balances.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class InventoryBalancesPageComponent implements OnInit {
  readonly title = 'Stock balances';
  readonly description = 'Review current quantity and inventory value by item and warehouse.';
  rows: InventoryBalance[] = [];
  loading = false;
  error = '';

  constructor(
    private readonly balances: InventoryBalanceService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loading = true;
    this.balances.getAll().subscribe({
      next: rows => {
        this.rows = rows;
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
