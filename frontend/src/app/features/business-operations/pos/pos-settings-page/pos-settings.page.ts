import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../../../core/auth.service';
import { SearchableSelectDirective } from '../../../../shared/directives/searchable-select.directive';
import { Choice } from '../../../../shared/models/shared.models';
import { OptionDataService } from '../../../../shared/data/option-data.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { PosSettings } from '../pos.model';
import { PosService } from '../pos.service';

@Component({
  selector: 'app-pos-settings-page',
  imports: [FormsModule, RouterLink, SearchableSelectDirective],
  templateUrl: './pos-settings.page.html',
  styleUrl: './pos-settings.page.css',
})
export class PosSettingsPageComponent implements OnInit {
  readonly notifications = inject(NotificationService);
  readonly title = 'POS settings';
  options: Record<string, Choice[]> = {};
  loading = true;
  saving = false;
  hasSavedSetup = false;
  editMode = false;
  error = '';
  setupError = '';
  private savedSettings: PosSettings | null = null;

  warehouseId: number | null = null;
  journalId: number | null = null;
  accountingPeriodId: number | null = null;
  revenueAccountId: number | null = null;
  receivableAccountId: number | null = null;
  settlementAccountId: number | null = null;
  taxCodeId: number | null = null;
  taxAccountId: number | null = null;
  inventoryAccountId: number | null = null;
  costOfGoodsSoldAccountId: number | null = null;

  constructor(
    readonly auth: AuthService,
    private readonly optionData: OptionDataService,
    private readonly pos: PosService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    if (!this.auth.companyId()) {
      this.error = 'Choose an organization before configuring POS settings.';
      this.loading = false;
      return;
    }

    forkJoin({
      warehouses: this.optionData.getOptions('warehouses'),
      journals: this.optionData.getOptions('journals'),
      periods: this.optionData.getOptions('periods'),
      revenueAccounts: this.optionData.getAccountOptionsByType('Revenue'),
      assetAccounts: this.optionData.getAccountOptionsByType('Asset'),
      taxAccounts: this.optionData.getAccountOptionsByType('Liability'),
      expenseAccounts: this.optionData.getAccountOptionsByType('Expense'),
      taxCodes: this.optionData.getOptions('taxCodes'),
      settings: this.pos.getSettings(),
    }).subscribe({
      next: (result) => {
        this.options = {
          warehouses: result.warehouses,
          journals: result.journals,
          periods: result.periods.filter((option) => option.label.endsWith('· Open')),
          revenueAccounts: result.revenueAccounts,
          assetAccounts: result.assetAccounts,
          taxAccounts: result.taxAccounts,
          expenseAccounts: result.expenseAccounts,
          taxCodes: result.taxCodes,
        };
        this.setSetupValues(result.settings);
        this.hasSavedSetup = this.setupComplete;
        this.savedSettings = this.hasSavedSetup ? this.currentSettings() : null;
        this.editMode = !this.hasSavedSetup;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: (error) => {
        this.error = errorMessage(error, 'Could not load POS settings.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  get setupComplete(): boolean {
    return (
      this.hasOption('warehouses', this.warehouseId) &&
      this.hasOption('journals', this.journalId) &&
      this.hasOption('periods', this.accountingPeriodId) &&
      this.hasOption('revenueAccounts', this.revenueAccountId) &&
      this.hasOption('assetAccounts', this.receivableAccountId) &&
      this.hasOption('assetAccounts', this.settlementAccountId) &&
      this.hasOption('assetAccounts', this.inventoryAccountId) &&
      this.hasOption('expenseAccounts', this.costOfGoodsSoldAccountId) &&
      (this.taxCodeId === null ||
        (this.hasOption('taxCodes', this.taxCodeId) &&
          this.hasOption('taxAccounts', this.taxAccountId)))
    );
  }

  onSetupChanged(): void {
    this.setupError = '';
    if (this.taxCodeId === null) this.taxAccountId = null;
  }

  saveSetup(): void {
    if (!this.auth.isAdministrator()) {
      this.setupError = 'Only company administrators can update POS defaults.';
      return;
    }
    if (!this.setupComplete) {
      this.setupError = 'Complete all required POS settings before saving.';
      return;
    }

    this.saving = true;
    this.setupError = '';
    this.pos.saveSettings(this.currentSettings()).subscribe({
      next: (settings) => {
        this.setSetupValues(settings);
        this.hasSavedSetup = this.setupComplete;
        this.savedSettings = this.hasSavedSetup ? this.currentSettings() : null;
        this.editMode = !this.hasSavedSetup;
        this.saving = false;
        this.notifications.success('POS settings saved for this company.');
        this.changeDetector.markForCheck();
      },
      error: (error) => {
        this.setupError = errorMessage(error, 'Could not save POS settings.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  cancelChanges(): void {
    if (!this.savedSettings) return;
    this.setSetupValues(this.savedSettings);
    this.editMode = false;
    this.setupError = '';
  }

  private hasOption(source: string, value: number | null): boolean {
    return (
      value !== null &&
      Number.isSafeInteger(value) &&
      value > 0 &&
      (this.options[source] ?? []).some((option) => Number(option.value) === value)
    );
  }

  private currentSettings(): PosSettings {
    return {
      warehouseId: this.warehouseId,
      journalId: this.journalId,
      accountingPeriodId: this.accountingPeriodId,
      revenueAccountId: this.revenueAccountId,
      receivableAccountId: this.receivableAccountId,
      settlementAccountId: this.settlementAccountId,
      taxCodeId: this.taxCodeId,
      taxAccountId: this.taxAccountId,
      inventoryAccountId: this.inventoryAccountId,
      costOfGoodsSoldAccountId: this.costOfGoodsSoldAccountId,
    };
  }

  private setSetupValues(settings: PosSettings): void {
    this.warehouseId = settings.warehouseId;
    this.journalId = settings.journalId;
    this.accountingPeriodId = settings.accountingPeriodId;
    this.revenueAccountId = settings.revenueAccountId;
    this.receivableAccountId = settings.receivableAccountId;
    this.settlementAccountId = settings.settlementAccountId;
    this.taxCodeId = settings.taxCodeId;
    this.taxAccountId = settings.taxAccountId;
    this.inventoryAccountId = settings.inventoryAccountId;
    this.costOfGoodsSoldAccountId = settings.costOfGoodsSoldAccountId;
  }
}
