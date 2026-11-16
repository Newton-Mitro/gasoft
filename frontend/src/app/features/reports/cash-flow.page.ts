import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { errorMessage } from '../../shared/utils/page-utils';
import { CashFlowClassification, CashFlowReport } from './report.model';
import { FinancialReportService } from './report.service';

@Component({
  selector: 'app-cash-flow-page',
  imports: [FormsModule],
  templateUrl: './cash-flow.page.html',
  styleUrls: ['../../shared/styles/feature-page.css', './cash-flow.page.css'],
})
export class CashFlowPageComponent implements OnInit {
  fromDate = `${new Date().getUTCFullYear()}-01-01`;
  toDate = new Date().toISOString().slice(0, 10);
  report: CashFlowReport | null = null;
  classifications: CashFlowClassification[] = [];
  method: 'direct' | 'indirect' = 'direct';
  loading = false;
  classificationsLoading = false;
  error = '';

  constructor(
    private readonly reports: FinancialReportService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadReport();
  }

  onDateChange(): void {
    this.loadReport();
  }

  formatAmount(amount: number): string {
    if (!Number.isFinite(amount)) return '—';
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: this.report?.currencyCode ?? 'USD',
      maximumFractionDigits: 2,
    }).format(amount);
  }

  updateClassification(account: CashFlowClassification, category: string): void {
    const selectedCategory = category || null;
    this.classificationsLoading = true;
    this.error = '';
    this.reports.updateCashFlowClassification(account.accountId, selectedCategory).subscribe({
      next: () => {
        this.classifications = this.classifications.map(item =>
          item.accountId === account.accountId ? { ...item, cashFlowCategory: selectedCategory } : item);
        this.classificationsLoading = false;
        this.loadReport();
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save this account classification.');
        this.classificationsLoading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadReport(): void {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(this.fromDate) ||
      !/^\d{4}-\d{2}-\d{2}$/.test(this.toDate)) return;
    if (this.fromDate > this.toDate) {
      this.error = 'The end date must be on or after the start date.';
      return;
    }
    this.loading = true;
    this.error = '';
    forkJoin({
      report: this.reports.getCashFlow({ fromDate: this.fromDate, toDate: this.toDate }),
      classifications: this.reports.getCashFlowClassifications(),
    }).subscribe({
      next: result => {
        this.report = result.report;
        this.classifications = result.classifications;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load the cash flow report.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}
