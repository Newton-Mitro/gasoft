import { HttpParams } from '@angular/common/http';
import { DecimalPipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit, signal, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, Subscription } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { errorMessage } from '../../shared/utils/page-utils';
import { DataRow } from '../../shared/models/shared.models';
import { NotificationService } from '../../shared/services/notification.service';
import { FinancialReportService } from '../reports/data-access/report.service';
import { IncomeStatementReport } from '../reports/data-access/report.model';

interface DashboardSummary {
  currencyCode: string;
  totalAccounts: number;
  totalEntries: number;
  totalAssets: number;
  totalLiabilitiesAndEquity: number;
  netIncome: number;
  totalRevenue: number;
  totalExpenses: number;
}

interface DashboardActivity {
  id: number;
  entryDate: string;
  description: string;
  status: string;
  totalDebit: number;
  totalCredit: number;
}

interface DashboardTrend {
  label: string;
  revenue: number;
  expenses: number;
  revenueHeight: number;
  expensesHeight: number;
}

interface PageResult<T> {
  items: T[];
  totalCount: number;
}

interface JournalEntryRecord {
  id: number;
  entryDate: string;
  description: string;
  status: string;
  totalDebit: number;
  totalCredit: number;
}

@Component({
  selector: 'app-dashboard-page',
  imports: [RouterLink, DecimalPipe],
  styleUrls: [
    '../../shared/styles/feature-page.css',
    './dashboard.page.css',
  ],
  templateUrl: './dashboard.page.html',
})
export class DashboardPage implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly summary = signal<DashboardSummary | null>(null);
  readonly activity = signal<DashboardActivity[]>([]);
  readonly trend = signal<DashboardTrend[]>([]);
  readonly currentYear = new Date().getFullYear();
  private requestSubscription?: Subscription;

  constructor(
    private readonly api: ApiService,
    private readonly reports: FinancialReportService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  ngOnDestroy(): void {
    this.requestSubscription?.unsubscribe();
  }

  get today(): string {
    return new Intl.DateTimeFormat(undefined, {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      year: 'numeric',
    }).format(new Date());
  }

  get greeting(): string {
    const hour = new Date().getHours();
    return hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening';
  }

  currency(value: number): string {
    if (!Number.isFinite(value)) return '—';
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: this.summary()?.currencyCode ?? 'USD',
      maximumFractionDigits: 0,
    }).format(value);
  }

  formatDate(value: string): string {
    const date = new Date(`${value}T00:00:00`);
    return Number.isNaN(date.getTime())
      ? value
      : new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' }).format(date);
  }

  activityStatus(status: string): string {
    return status.toLowerCase() === 'posted' ? 'posted' : 'draft';
  }

  retry(): void {
    this.error.set('');
    this.loading.set(true);
    this.loadDashboard();
  }

  private loadDashboard(): void {
    const now = new Date();
    const yearStart = `${now.getFullYear()}-01-01`;
    const today = this.toDateKey(now);
    const months = Array.from({ length: now.getMonth() + 1 }, (_, month) => {
      const monthStart = new Date(now.getFullYear(), month, 1);
      const monthEnd = new Date(now.getFullYear(), month + 1, 0);
      return {
        label: new Intl.DateTimeFormat(undefined, { month: 'short' }).format(monthStart),
        fromDate: this.toDateKey(monthStart),
        toDate: this.toDateKey(monthEnd > now ? now : monthEnd),
      };
    });

    this.requestSubscription?.unsubscribe();
    this.requestSubscription = forkJoin({
      trial: this.api.get<DataRow>('reports/trial-balance'),
      income: this.reports.getIncomeStatement({ fromDate: yearStart, toDate: today }),
      balance: this.api.get<DataRow>('reports/balance-sheet'),
      accounts: this.api.get<PageResult<unknown>>(
        'accounts/page',
        true,
        new HttpParams().set('page', '1').set('pageSize', '1'),
      ),
      entries: this.api.get<PageResult<JournalEntryRecord>>(
        'journal-entries/page',
        true,
        new HttpParams().set('page', '1').set('pageSize', '6'),
      ),
      monthly: forkJoin(months.map(month =>
        this.reports.getIncomeStatement({ fromDate: month.fromDate, toDate: month.toDate }),
      )),
    }).subscribe({
      next: result => {
        this.summary.set({
          currencyCode: String(result.trial['currencyCode'] ?? result.income.currencyCode ?? 'USD'),
          totalAccounts: this.numberValue(result.accounts.totalCount),
          totalEntries: this.numberValue(result.entries.totalCount),
          totalAssets: this.numberValue(result.balance['totalAssets']),
          totalLiabilitiesAndEquity: this.numberValue(result.balance['totalLiabilitiesAndEquity']),
          netIncome: result.income.netIncome,
          totalRevenue: result.income.totalRevenue,
          totalExpenses: result.income.totalExpenses,
        });
        this.activity.set(result.entries.items.map(entry => ({
          id: entry.id,
          entryDate: entry.entryDate,
          description: entry.description,
          status: entry.status,
          totalDebit: entry.totalDebit,
          totalCredit: entry.totalCredit,
        })));
        this.trend.set(this.buildTrend(months.map((month, index) => ({
          label: month.label,
          report: result.monthly[index],
        }))));
        this.loading.set(false);
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error.set(errorMessage(error, 'Could not load your workspace overview.'));
        this.loading.set(false);
        this.changeDetector.markForCheck();
      },
    });
  }

  private buildTrend(months: { label: string; report: IncomeStatementReport | undefined }[]): DashboardTrend[] {
    const values = months.map(month => ({
      label: month.label,
      revenue: month.report?.totalRevenue ?? 0,
      expenses: month.report?.totalExpenses ?? 0,
    }));
    const maximum = Math.max(1, ...values.flatMap(month => [month.revenue, month.expenses]));
    return values.map(month => ({
      ...month,
      revenueHeight: Math.max(month.revenue > 0 ? 4 : 0, month.revenue / maximum * 100),
      expensesHeight: Math.max(month.expenses > 0 ? 4 : 0, month.expenses / maximum * 100),
    }));
  }

  private numberValue(value: unknown): number {
    const number = Number(value);
    return Number.isFinite(number) ? number : 0;
  }

  private toDateKey(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
