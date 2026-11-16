import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import {
  FixedAsset,
  FixedAssetAccount,
  FixedAssetCreatePayload,
  FixedAssetPeriod,
  FixedAssetSetup,
} from './fixed-assets.model';
import { FixedAssetsService } from './fixed-assets.service';

@Component({
  selector: 'app-fixed-assets-page',
  imports: [FormsModule],
  templateUrl: './fixed-assets.page.html',
  styleUrls: ['../../../shared/styles/feature-page.css', './fixed-assets.page.css'],
})
export class FixedAssetsPageComponent implements OnInit {
  assets: FixedAsset[] = [];
  periods: FixedAssetPeriod[] = [];
  journals: FixedAssetSetup['journals'] = [];
  accounts: FixedAssetAccount[] = [];
  depreciationHistory: FixedAssetSetup['depreciationHistory'] = [];
  currencyCode = 'USD';
  creating = false;
  depreciatingAsset: FixedAsset | null = null;
  disposingAsset: FixedAsset | null = null;
  loading = true;
  saving = false;
  error = '';
  success = '';

  assetNumber = '';
  name = '';
  description = '';
  acquisitionDate = new Date().toISOString().slice(0, 10);
  inServiceDate = new Date().toISOString().slice(0, 10);
  acquisitionCost: number | null = null;
  residualValue: number | null = 0;
  usefulLifeMonths: number | null = 60;
  acquisitionPeriodId: number | null = null;
  acquisitionJournalId: number | null = null;
  assetAccountId: number | null = null;
  accumulatedDepreciationAccountId: number | null = null;
  depreciationExpenseAccountId: number | null = null;
  acquisitionCreditAccountId: number | null = null;

  depreciationThroughPeriodId: number | null = null;
  depreciationJournalId: number | null = null;

  disposalPeriodId: number | null = null;
  disposalJournalId: number | null = null;
  disposalDate = '';
  disposalProceeds: number | null = 0;
  disposalCashAccountId: number | null = null;
  disposalGainAccountId: number | null = null;
  disposalLossAccountId: number | null = null;

  constructor(
    private readonly service: FixedAssetsService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  get assetAccounts(): FixedAssetAccount[] {
    return this.accounts.filter(account => account.accountType === 'Asset');
  }

  get expenseAccounts(): FixedAssetAccount[] {
    return this.accounts.filter(account => account.accountType === 'Expense');
  }

  get revenueAccounts(): FixedAssetAccount[] {
    return this.accounts.filter(account => account.accountType === 'Revenue');
  }

  get fundingAccounts(): FixedAssetAccount[] {
    return this.accounts.filter(account => account.accountType === 'Asset' || account.accountType === 'Liability');
  }

  get openPeriods(): FixedAssetPeriod[] {
    return this.periods.filter(period => period.status === 'Open');
  }

  beginCreate(): void {
    this.resetForms();
    this.creating = true;
    this.error = '';
    this.success = '';
  }

  create(): void {
    if (!this.assetNumber.trim() || !this.name.trim() || !this.acquisitionDate || !this.inServiceDate ||
      this.acquisitionCost === null || !Number.isFinite(Number(this.acquisitionCost)) ||
      this.residualValue === null || !Number.isFinite(Number(this.residualValue)) ||
      this.usefulLifeMonths === null || !Number.isInteger(Number(this.usefulLifeMonths)) ||
      !this.acquisitionPeriodId || !this.acquisitionJournalId || !this.assetAccountId ||
      !this.accumulatedDepreciationAccountId || !this.depreciationExpenseAccountId ||
      !this.acquisitionCreditAccountId) {
      this.error = 'Complete all required asset, account, journal, and accounting-period details.';
      return;
    }
    const payload: FixedAssetCreatePayload = {
      assetNumber: this.assetNumber.trim(),
      name: this.name.trim(),
      description: this.description.trim() || null,
      acquisitionDate: this.acquisitionDate,
      inServiceDate: this.inServiceDate,
      acquisitionCost: Number(this.acquisitionCost),
      residualValue: Number(this.residualValue),
      usefulLifeMonths: Number(this.usefulLifeMonths),
      accountingPeriodId: Number(this.acquisitionPeriodId),
      journalId: Number(this.acquisitionJournalId),
      assetAccountId: Number(this.assetAccountId),
      accumulatedDepreciationAccountId: Number(this.accumulatedDepreciationAccountId),
      depreciationExpenseAccountId: Number(this.depreciationExpenseAccountId),
      creditAccountId: Number(this.acquisitionCreditAccountId),
    };
    this.submit(() => this.service.create(payload), 'Asset acquired and posted to the ledger.', () => {
      this.creating = false;
      this.load();
    });
  }

  beginDepreciation(asset: FixedAsset): void {
    this.creating = false;
    this.disposingAsset = null;
    this.depreciatingAsset = asset;
    this.depreciationThroughPeriodId = this.openPeriods
      .filter(period => period.endsOn >= asset.inServiceDate)
      .at(0)?.id ?? null;
    this.depreciationJournalId = null;
    this.error = '';
    this.success = '';
  }

  postDepreciation(): void {
    if (!this.depreciatingAsset || !this.depreciationThroughPeriodId || !this.depreciationJournalId) {
      this.error = 'Choose an open period and journal for depreciation.';
      return;
    }
    const asset = this.depreciatingAsset;
    this.submit(
      () => this.service.depreciate(asset.id, Number(this.depreciationThroughPeriodId), Number(this.depreciationJournalId)),
      'Depreciation posted for all due open periods through the selected period.',
      () => {
        this.depreciatingAsset = null;
        this.load();
      },
    );
  }

  beginDisposal(asset: FixedAsset): void {
    this.creating = false;
    this.depreciatingAsset = null;
    this.disposingAsset = asset;
    const period = this.openPeriods.find(candidate => candidate.endsOn >= asset.inServiceDate);
    this.disposalPeriodId = period?.id ?? null;
    this.disposalDate = period?.endsOn ?? '';
    this.disposalProceeds = 0;
    this.disposalJournalId = null;
    this.disposalCashAccountId = null;
    this.disposalGainAccountId = null;
    this.disposalLossAccountId = null;
    this.error = '';
    this.success = '';
  }

  onDisposalPeriodChange(value: string | number): void {
    this.disposalPeriodId = Number(value);
    this.disposalDate = this.periods.find(period => period.id === this.disposalPeriodId)?.endsOn ?? '';
  }

  dispose(): void {
    const asset = this.disposingAsset;
    if (!asset || !this.disposalPeriodId || !this.disposalDate ||
      this.disposalProceeds === null || !Number.isFinite(Number(this.disposalProceeds)) ||
      this.disposalProceeds < 0 || !this.disposalJournalId ||
      (this.disposalProceeds > 0 && !this.disposalCashAccountId) ||
      !this.disposalGainAccountId || !this.disposalLossAccountId) {
      this.error = 'Complete the disposal date, proceeds, journal, and gain/loss accounts.';
      return;
    }
    this.submit(() => this.service.dispose(asset.id, {
      disposalDate: this.disposalDate,
      proceeds: Number(this.disposalProceeds),
      accountingPeriodId: Number(this.disposalPeriodId),
      journalId: Number(this.disposalJournalId),
      cashAccountId: Number(this.disposalCashAccountId ?? 0),
      gainAccountId: Number(this.disposalGainAccountId),
      lossAccountId: Number(this.disposalLossAccountId),
    }), 'Asset disposed and gain or loss posted to the ledger.', () => {
      this.disposingAsset = null;
      this.load();
    });
  }

  cancel(): void {
    this.creating = false;
    this.depreciatingAsset = null;
    this.disposingAsset = null;
    this.error = '';
  }

  formatCurrency(value: number): string {
    if (!Number.isFinite(value)) return '—';
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: this.currencyCode,
      maximumFractionDigits: 2,
    }).format(value);
  }

  assetDepreciation(assetId: number): FixedAssetSetup['depreciationHistory'] {
    return this.depreciationHistory.filter(item => item.assetId === assetId);
  }

  private submit(
    request: () => Observable<unknown>,
    message: string,
    onSuccess: () => void,
  ): void {
    this.saving = true;
    this.error = '';
    this.success = '';
    request().subscribe({
      next: () => {
        this.saving = false;
        this.success = message;
        onSuccess();
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not complete the fixed-asset operation.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private load(): void {
    this.loading = true;
    this.service.getAll().subscribe({
      next: result => {
        this.applySetup(result);
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load fixed assets.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private applySetup(result: FixedAssetSetup): void {
    this.currencyCode = result.currencyCode;
    this.assets = result.assets;
    this.periods = result.periods;
    this.journals = result.journals;
    this.accounts = result.accounts;
    this.depreciationHistory = result.depreciationHistory;
  }

  private resetForms(): void {
    this.depreciatingAsset = null;
    this.disposingAsset = null;
    this.assetNumber = '';
    this.name = '';
    this.description = '';
    this.acquisitionDate = new Date().toISOString().slice(0, 10);
    this.inServiceDate = this.acquisitionDate;
    this.acquisitionCost = null;
    this.residualValue = 0;
    this.usefulLifeMonths = 60;
    this.acquisitionPeriodId = this.openPeriods[0]?.id ?? null;
    this.acquisitionJournalId = null;
    this.assetAccountId = null;
    this.accumulatedDepreciationAccountId = null;
    this.depreciationExpenseAccountId = null;
    this.acquisitionCreditAccountId = null;
  }
}
