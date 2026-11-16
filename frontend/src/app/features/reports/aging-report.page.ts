import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { errorMessage } from '../../shared/utils/page-utils';
import { AgingReport } from './report.model';
import { FinancialReportService } from './report.service';

type AgingReportKind = 'receivables' | 'payables';

@Component({
  selector: 'app-aging-report-page',
  imports: [FormsModule],
  templateUrl: './aging-report.page.html',
  styleUrl: './aging-report.page.css',
})
export class AgingReportPageComponent implements OnInit {
  kind: AgingReportKind = 'receivables';
  asOfDate = '';
  report: AgingReport | null = null;
  loading = false;
  error = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly reports: FinancialReportService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    const kind = this.route.snapshot.data['agingKind'];
    if (kind !== 'receivables' && kind !== 'payables') {
      this.error = 'This aging report is not configured correctly.';
      return;
    }
    this.kind = kind;
    this.asOfDate = new Date().toISOString().slice(0, 10);
    this.loadReport();
  }

  get title(): string {
    return this.kind === 'receivables' ? 'Accounts receivable aging' : 'Accounts payable aging';
  }

  get documentLabel(): string {
    return this.kind === 'receivables' ? 'Invoice' : 'Bill';
  }

  formatAmount(amount: number, currencyCode: string): string {
    if (!Number.isFinite(amount)) return '—';
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: currencyCode,
      maximumFractionDigits: 2,
    }).format(amount);
  }

  onDateChange(): void {
    this.loadReport();
  }

  private loadReport(): void {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(this.asOfDate)) return;
    this.loading = true;
    this.error = '';
    const request: Observable<AgingReport> = this.kind === 'receivables'
      ? this.reports.getReceivablesAging(this.asOfDate)
      : this.reports.getPayablesAging(this.asOfDate);
    request.subscribe({
      next: report => {
        this.report = report;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, `Could not load the ${this.title.toLowerCase()}.`);
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}
