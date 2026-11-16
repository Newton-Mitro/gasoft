import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { errorMessage } from '../../shared/utils/page-utils';
import {
  BudgetAccount,
  BudgetChoice,
  BudgetLineInput,
  BudgetPeriod,
  BudgetReportAccount,
  BudgetSetup,
  BudgetVsActualReport,
} from './budget.model';
import { BudgetService } from './budget.service';

interface EditableBudgetAccount extends BudgetAccount {
  amounts: Record<number, number>;
}

@Component({
  selector: 'app-budget-page',
  imports: [FormsModule],
  templateUrl: './budget.page.html',
  styleUrl: './budget.page.css',
})
export class BudgetPageComponent implements OnInit {
  fiscalYears: BudgetSetup['fiscalYears'] = [];
  budgets: BudgetChoice[] = [];
  periods: BudgetPeriod[] = [];
  accounts: EditableBudgetAccount[] = [];
  report: BudgetVsActualReport | null = null;
  selectedFiscalYearId: number | null = null;
  selectedBudgetId: number | null = null;
  selectedPeriodId: number | null = null;
  newBudgetName = '';
  view: 'plan' | 'actual' = 'plan';
  loading = true;
  saving = false;
  error = '';
  success = '';

  constructor(
    private readonly budgetsService: BudgetService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadSetup();
  }

  onFiscalYearChange(): void {
    this.loadSetup(this.selectedFiscalYearId ?? undefined);
  }

  onBudgetChange(): void {
    this.report = null;
    this.accounts = [];
    if (this.selectedBudgetId === null) return;
    this.loadReport(this.selectedBudgetId);
  }

  createBudget(): void {
    const name = this.newBudgetName.trim();
    if (!name || this.selectedFiscalYearId === null) {
      this.error = 'Enter a budget name and select a fiscal year.';
      return;
    }
    this.saving = true;
    this.error = '';
    this.success = '';
    this.budgetsService.create(name, this.selectedFiscalYearId).subscribe({
      next: budget => {
        this.newBudgetName = '';
        this.success = `Budget “${budget.name}” created.`;
        this.loadSetup(budget.fiscalYearId, budget.id);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not create the budget.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  saveBudget(): void {
    if (this.selectedBudgetId === null || !this.validAmounts()) {
      this.error = 'Budget amounts must be valid non-negative numbers.';
      return;
    }
    const lines: BudgetLineInput[] = this.accounts.flatMap(account =>
      this.periods.map(period => ({
        accountId: account.accountId,
        accountingPeriodId: period.id,
        amount: Number(account.amounts[period.id] ?? 0),
      })).filter(line => line.amount > 0));
    this.saving = true;
    this.error = '';
    this.success = '';
    this.budgetsService.saveLines(this.selectedBudgetId, lines).subscribe({
      next: () => {
        this.success = 'Budget amounts saved.';
        this.loadReport(this.selectedBudgetId!);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save budget amounts.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  deleteBudget(): void {
    if (this.selectedBudgetId === null) return;
    const selected = this.budgets.find(budget => budget.id === this.selectedBudgetId);
    if (!selected || !window.confirm(`Delete “${selected.name}” and all of its budget amounts?`)) return;
    this.saving = true;
    this.error = '';
    this.success = '';
    this.budgetsService.delete(selected.id).subscribe({
      next: () => {
        this.success = `Budget “${selected.name}” deleted.`;
        this.loadSetup(this.selectedFiscalYearId ?? undefined);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not delete the budget.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  onPeriodChange(value: string | number): void {
    this.selectedPeriodId = Number(value);
  }

  formatCurrency(amount: number): string {
    if (!Number.isFinite(amount)) return '—';
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: this.report?.currencyCode ?? 'USD',
      maximumFractionDigits: 2,
    }).format(amount);
  }

  formatPeriod(period: BudgetPeriod): string {
    const dateFormat = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
    const start = dateFormat.format(new Date(`${period.startsOn}T00:00:00`));
    const end = dateFormat.format(new Date(`${period.endsOn}T00:00:00`));
    return start === end ? start : `${start} – ${end}`;
  }

  get selectedPeriod(): BudgetPeriod | undefined {
    return this.periods.find(period => period.id === this.selectedPeriodId);
  }

  periodValue(account: BudgetReportAccount): BudgetReportAccount['periods'][number] | undefined {
    return account.periods.find(value => value.periodId === this.selectedPeriodId);
  }

  private loadSetup(fiscalYearId?: number, preferredBudgetId?: number): void {
    this.loading = true;
    this.saving = false;
    this.error = '';
    this.budgetsService.getSetup(fiscalYearId).subscribe({
      next: setup => {
        this.fiscalYears = setup.fiscalYears;
        this.selectedFiscalYearId = fiscalYearId ?? setup.fiscalYears[0]?.id ?? null;
        this.budgets = setup.budgets;
        this.periods = setup.periods;
        this.accounts = setup.accounts.map(account => ({ ...account, amounts: {} }));
        this.selectedBudgetId = preferredBudgetId && this.budgets.some(budget => budget.id === preferredBudgetId)
          ? preferredBudgetId
          : this.budgets[0]?.id ?? null;
        this.selectedPeriodId = this.periods[0]?.id ?? null;
        if (this.selectedBudgetId !== null) {
          this.loadReport(this.selectedBudgetId);
          return;
        }
        this.report = null;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load budgets.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadReport(budgetId: number): void {
    this.loading = true;
    this.budgetsService.getBudgetVsActual(budgetId).subscribe({
      next: report => {
        this.report = report;
        this.periods = report.periods;
        const savedAmounts = new Map(report.accounts.map(account => [account.accountId, account.periods]));
        this.accounts = this.accounts.map(account => ({
          ...account,
          amounts: Object.fromEntries(report.periods.map(period => [
            period.id,
            savedAmounts.get(account.accountId)?.find(value => value.periodId === period.id)?.budget ?? 0,
          ])),
        }));
        if (!this.periods.some(period => period.id === this.selectedPeriodId)) {
          this.selectedPeriodId = this.periods[0]?.id ?? null;
        }
        this.loading = false;
        this.saving = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load the budget-vs-actual report.');
        this.loading = false;
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private validAmounts(): boolean {
    return this.accounts.every(account => this.periods.every(period => {
      const amount = Number(account.amounts[period.id] ?? 0);
      return Number.isFinite(amount) && amount >= 0;
    }));
  }
}
