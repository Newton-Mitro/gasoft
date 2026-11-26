import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { Subscription, forkJoin } from 'rxjs';
import { JournalEntryLinesEditorComponent } from '../../../../shared/components/journal-entry-lines-editor/journal-entry-lines-editor.component';
import { OptionDataService } from '../../../../shared/data/option-data.service';
import { Choice, JournalEntryLineForm } from '../../../../shared/models/shared.models';
import { NotificationService } from '../../../../shared/services/notification.service';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { isValidJournalEntryLine } from '../../journal-entries/journal-entry-lines';
import { FiscalYear, InitialOpeningBalanceLine } from '../fiscal-year.model';
import { FiscalYearService } from '../fiscal-year.service';

@Component({
  selector: 'app-starting-balances-page',
  imports: [RouterLink, JournalEntryLinesEditorComponent],
  templateUrl: './starting-balances.page.html',
  styleUrl: '../../../../shared/styles/feature-page.css',
  styles: [`
    .starting-balances-panel {
      overflow: hidden;
      margin-bottom: 16px;
    }

    .starting-balances-content {
      padding: 20px;
    }

    .starting-balances-content > p {
      margin: 0;
      color: var(--text-muted);
      font-size: 11px;
      line-height: 1.65;
    }

    .account-load-retry {
      margin-top: 12px;
    }

    app-journal-entry-lines-editor {
      display: block;
      margin-top: 18px;
    }

    .starting-balances-actions {
      margin-top: 18px;
      padding-top: 16px;
      border-top: 1px solid var(--border);
    }

    .success-state {
      padding: 34px 24px;
    }

    .success-icon {
      color: var(--success-text);
      background: #e8f5eb;
    }

    .success-actions {
      justify-content: center;
      margin-top: 16px;
    }

    @media (max-width: 600px) {
      .starting-balances-content {
        padding: 15px;
      }

      .success-actions {
        align-items: stretch;
        flex-direction: column;
      }
    }
  `],
})
export class StartingBalancesPageComponent implements OnInit, OnDestroy {
  readonly indexPath = '/general-ledger/fiscal-years';
  year: FiscalYear | null = null;
  lines: JournalEntryLineForm[] = [emptyOpeningBalanceLine()];
  accountOptions: Choice[] = [];
  loading = true;
  accountsLoading = false;
  accountsFailed = false;
  saving = false;
  error = '';
  result: { id: number; status: string } | null = null;

  private routeSubscription?: Subscription;
  private requestSubscription = new Subscription();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly fiscalYears: FiscalYearService,
    private readonly optionData: OptionDataService,
    readonly notifications: NotificationService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = this.route.paramMap.subscribe(params => this.loadFiscalYear(params));
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
    this.requestSubscription.unsubscribe();
  }

  get canCreate(): boolean {
    const accountIds = this.lines.map(line => line.accountId);
    return !this.saving &&
      !this.accountsLoading &&
      !this.accountsFailed &&
      this.accountOptions.length > 0 &&
      this.lines.length > 0 &&
      this.lines.every(isValidJournalEntryLine) &&
      this.lines.every(line =>
        line.accountId !== null && Number.isSafeInteger(line.accountId) && line.accountId > 0) &&
      new Set(accountIds).size === accountIds.length;
  }

  setLines(lines: JournalEntryLineForm[]): void {
    this.lines = lines;
  }

  retryLoadingAccounts(): void {
    this.loadAccountOptions();
  }

  createDraft(): void {
    if (!this.year || !this.canCreate) return;

    const lines: InitialOpeningBalanceLine[] = this.lines.map(line => ({
      accountId: Number(line.accountId),
      debit: Number(line.debit ?? 0),
      credit: Number(line.credit ?? 0),
    }));
    this.saving = true;
    this.error = '';
    this.requestSubscription.add(this.fiscalYears.prepareInitialOpeningBalances(this.year.id, lines).subscribe({
      next: entry => {
        this.saving = false;
        this.result = entry;
        this.notifications.success(`Starting-balance draft ${entry.id} created. Review and post it before using the ledger.`);
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.saving = false;
        this.error = errorMessage(error, 'Could not prepare starting balances.');
        this.notifications.error(this.error);
        this.changeDetector.markForCheck();
      },
    }));
  }

  cancel(): void {
    if (this.saving) return;
    void this.router.navigateByUrl(this.indexPath);
  }

  private loadFiscalYear(params: ParamMap): void {
    this.requestSubscription.unsubscribe();
    this.requestSubscription = new Subscription();
    this.year = null;
    this.result = null;
    this.lines = [emptyOpeningBalanceLine()];
    this.accountOptions = [];
    this.accountsLoading = false;
    this.accountsFailed = false;
    this.error = '';
    this.loading = true;

    const id = Number(params.get('recordId'));
    if (!Number.isSafeInteger(id) || id <= 0) {
      this.error = 'A valid fiscal year ID is required to set starting balances.';
      this.loading = false;
      return;
    }

    this.requestSubscription.add(forkJoin({
      year: this.fiscalYears.getById(id),
      years: this.fiscalYears.getAll(),
    }).subscribe({
      next: ({ year, years }) => {
        if (year.status !== 'Open') {
          this.error = 'Starting balances can only be set for an open fiscal year.';
          this.loading = false;
          this.changeDetector.markForCheck();
          return;
        }
        if (years.some(candidate => candidate.id !== year.id && candidate.endsOn < year.startsOn)) {
          this.error = 'Starting balances are only set for the first fiscal year. Use carry forward balances for later years.';
          this.loading = false;
          this.changeDetector.markForCheck();
          return;
        }

        this.year = year;
        this.loading = false;
        this.loadAccountOptions();
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load this fiscal year.');
        this.loading = false;
        this.notifications.error(this.error);
        this.changeDetector.markForCheck();
      },
    }));
  }

  private loadAccountOptions(): void {
    this.accountsLoading = true;
    this.accountsFailed = false;
    this.requestSubscription.add(this.optionData.getOpeningBalanceAccountOptions().subscribe({
      next: options => {
        this.accountOptions = options;
        this.accountsLoading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.accountsLoading = false;
        this.accountsFailed = true;
        this.notifications.error(errorMessage(error, 'Could not load balance-sheet accounts.'));
        this.changeDetector.markForCheck();
      },
    }));
  }
}

function emptyOpeningBalanceLine(): JournalEntryLineForm {
  return { accountId: null, debit: null, credit: null };
}
