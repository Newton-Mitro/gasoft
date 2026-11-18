import { ChangeDetectorRef, Component, Input, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { FinancialReportService } from '../data-access/report.service';
import {
  BalanceSheetReport,
  FinancialReport,
  IncomeStatementReport,
  ReportAccount,
  ReportType,
  TrialBalanceReport,
} from '../data-access/report.model';
import { NotificationService } from '../../../shared/services/notification.service';
import { FieldValidationDirective } from '../../../shared/directives/field-validation.directive';
import { InputComponent } from '../../../shared/components/input/input.component';

interface SummaryCard {
  label: string;
  value: number;
  highlight?: boolean;
}

interface ReportRow {
  accountCode: string;
  accountName: string;
  accountType: string;
  amount: number;
  credit?: number;
}

interface ReportVisualMetric {
  label: string;
  value: number;
  width: number;
  tone: 'primary' | 'secondary' | 'tertiary';
}

@Component({
  selector: 'app-financial-report',
  imports: [FormsModule, FieldValidationDirective, InputComponent],
  templateUrl: './report-pages.component.html',
  styleUrls: [
    '../../../shared/styles/feature-page.css',
    './report-pages.component.css',
  ],
})
export class FinancialReportPageComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);

  @Input({ required: true }) reportType!: ReportType;
  title = '';
  description = '';
  rows: ReportRow[] = [];
  summaryCards: SummaryCard[] = [];
  visualMetrics: ReportVisualMetric[] = [];
  currencyCode = 'USD';
  fromDate = '';
  toDate = '';
  asOfDate = '';
  loading = false;
  error = '';
  private requestSubscription?: Subscription;

  constructor(
    private readonly reports: FinancialReportService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.asOfDate = new Date().toISOString().slice(0, 10);
    this.configureReport();
    this.loadReport();
  }

  ngOnDestroy(): void {
    this.requestSubscription?.unsubscribe();
  }

  onDateChange(): void {
    this.loadReport();
  }

  formatCurrency(value: number): string {
    if (!Number.isFinite(value)) return '—';
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: this.currencyCode,
      maximumFractionDigits: 2,
    }).format(value);
  }

  private configureReport(): void {
    const config: Record<ReportType, { title: string; description: string }> = {
      'trial-balance': {
        title: 'Trial balance',
        description: 'Review posted account balances for a selected period.',
      },
      'income-statement': {
        title: 'Income statement',
        description: 'Compare posted revenue and expenses for a date range.',
      },
      'balance-sheet': {
        title: 'Balance sheet',
        description: 'Review your company’s assets, liabilities, and equity as of a selected date.',
      },
    };
    const selected = config[this.reportType] ?? config['trial-balance'];
    this.title = selected.title;
    this.description = selected.description;
  }

  private loadReport(): void {
    this.loading = true;
    this.error = '';
    if (this.reportType === 'trial-balance') {
      this.executeReport(this.reports.getTrialBalance({ fromDate: this.fromDate, toDate: this.toDate }));
    } else if (this.reportType === 'income-statement') {
      this.executeReport(this.reports.getIncomeStatement({ fromDate: this.fromDate, toDate: this.toDate }));
    } else {
      this.executeReport(this.reports.getBalanceSheet({ asOfDate: this.asOfDate }));
    }
  }

  private executeReport<T extends FinancialReport>(request: Observable<T>): void {
    this.requestSubscription?.unsubscribe();
    this.requestSubscription = request.subscribe({
      next: report => {
        this.setReportData(report);
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

  private setReportData(report: FinancialReport): void {
    this.currencyCode = report.currencyCode;
    this.rows = report.accounts.map(account => this.toReportRow(account));
    if (this.reportType === 'trial-balance') {
      const trialBalance = report as TrialBalanceReport;
      this.summaryCards = [
        { label: 'Total debits', value: trialBalance.totalDebit },
        { label: 'Total credits', value: trialBalance.totalCredit },
      ];
      const maximum = Math.max(trialBalance.totalDebit, trialBalance.totalCredit, 1);
      this.visualMetrics = [
        { label: 'Debits', value: trialBalance.totalDebit, width: trialBalance.totalDebit / maximum * 100, tone: 'primary' },
        { label: 'Credits', value: trialBalance.totalCredit, width: trialBalance.totalCredit / maximum * 100, tone: 'secondary' },
      ];
    } else if (this.reportType === 'income-statement') {
      const incomeStatement = report as IncomeStatementReport;
      this.summaryCards = [
        { label: 'Total revenue', value: incomeStatement.totalRevenue },
        { label: 'Total expenses', value: incomeStatement.totalExpenses },
        { label: 'Net income', value: incomeStatement.netIncome, highlight: true },
      ];
      const maximum = Math.max(incomeStatement.totalRevenue, incomeStatement.totalExpenses, 1);
      this.visualMetrics = [
        { label: 'Revenue', value: incomeStatement.totalRevenue, width: incomeStatement.totalRevenue / maximum * 100, tone: 'primary' },
        { label: 'Expenses', value: incomeStatement.totalExpenses, width: incomeStatement.totalExpenses / maximum * 100, tone: 'secondary' },
        {
          label: 'Net income',
          value: incomeStatement.netIncome,
          width: Math.min(100, Math.abs(incomeStatement.netIncome) / maximum * 100),
          tone: 'tertiary',
        },
      ];
    } else {
      const balanceSheet = report as BalanceSheetReport;
      this.summaryCards = [
        { label: 'Total assets', value: balanceSheet.totalAssets },
        { label: 'Total liabilities', value: balanceSheet.totalLiabilities },
        { label: 'Liabilities & equity', value: balanceSheet.totalLiabilitiesAndEquity, highlight: true },
      ];
      const maximum = Math.max(balanceSheet.totalAssets, balanceSheet.totalLiabilities, balanceSheet.totalEquity, 1);
      this.visualMetrics = [
        { label: 'Assets', value: balanceSheet.totalAssets, width: balanceSheet.totalAssets / maximum * 100, tone: 'primary' },
        { label: 'Liabilities', value: balanceSheet.totalLiabilities, width: balanceSheet.totalLiabilities / maximum * 100, tone: 'secondary' },
        { label: 'Equity', value: balanceSheet.totalEquity, width: balanceSheet.totalEquity / maximum * 100, tone: 'tertiary' },
      ];
    }
  }

  private toReportRow(account: ReportAccount): ReportRow {
    if (this.reportType === 'trial-balance') {
      const trialBalanceAccount = account as TrialBalanceReport['accounts'][number];
      return {
        accountCode: account.accountCode,
        accountName: account.accountName,
        accountType: account.accountType,
        amount: trialBalanceAccount.debit,
        credit: trialBalanceAccount.credit,
      };
    }
    const amount = this.reportType === 'income-statement'
      ? (account as IncomeStatementReport['accounts'][number]).amount
      : (account as BalanceSheetReport['accounts'][number]).balance;
    return {
      accountCode: account.accountCode,
      accountName: account.accountName,
      accountType: account.accountType,
      amount,
    };
  }
}

@Component({
  selector: 'app-trial-balance-page',
  imports: [FinancialReportPageComponent],
  template: '<app-financial-report reportType="trial-balance" />',
})
export class TrialBalancePageComponent {}

@Component({
  selector: 'app-income-statement-page',
  imports: [FinancialReportPageComponent],
  template: '<app-financial-report reportType="income-statement" />',
})
export class IncomeStatementPageComponent {}

@Component({
  selector: 'app-balance-sheet-page',
  imports: [FinancialReportPageComponent],
  template: '<app-financial-report reportType="balance-sheet" />',
})
export class BalanceSheetPageComponent {}
