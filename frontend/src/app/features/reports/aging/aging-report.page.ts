import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { AgingReport } from '../data-access/report.model';
import { FinancialReportService } from '../data-access/report.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { InputComponent } from '../../../shared/components/input/input.component';

type AgingReportKind = 'receivables' | 'payables';

interface AgingSegment {
  label: string;
  amount: number;
  width: number;
  tone: 'current' | 'early' | 'middle' | 'late' | 'old';
}

@Component({
  selector: 'app-aging-report-page',
  imports: [FormsModule, FieldValidationDirective, InputComponent],
  templateUrl: './aging-report.page.html',
  styleUrls: ['../../../shared/styles/feature-page.css', './aging-report.page.css'],
})
export class AgingReportPageComponent implements OnInit {
  readonly notifications = inject(NotificationService);
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

  agingSegments(totals: AgingReport['totalsByCurrency'][number]): AgingSegment[] {
    const buckets: Omit<AgingSegment, 'width'>[] = [
      { label: 'Current', amount: totals.current, tone: 'current' },
      { label: '1–30 days', amount: totals.days1To30, tone: 'early' },
      { label: '31–60 days', amount: totals.days31To60, tone: 'middle' },
      { label: '61–90 days', amount: totals.days61To90, tone: 'late' },
      { label: 'Over 90 days', amount: totals.over90Days, tone: 'old' },
    ];
    const denominator = Math.max(Math.abs(totals.total), 1);
    return buckets.map(bucket => ({
      ...bucket,
      width: Math.min(100, Math.abs(bucket.amount) / denominator * 100),
    }));
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
        this.error = '';
        this.notifications.error(errorMessage(error, `Could not load the ${this.title.toLowerCase()}.`));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}
