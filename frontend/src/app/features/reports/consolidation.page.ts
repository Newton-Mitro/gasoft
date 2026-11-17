import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { errorMessage } from '../../shared/utils/page-utils';
import {
  ConsolidationElimination,
  ConsolidationCompany,
  ConsolidationGroup,
  ConsolidationRateInput,
  ConsolidationRun,
  ConsolidationRunSummary,
} from './consolidation.model';
import { ConsolidationService } from './consolidation.service';
import { NotificationService } from '../../shared/services/notification.service';
import { FieldValidationDirective } from '../../shared/directives/field-validation.directive';

@Component({
  selector: 'app-consolidation-page',
  imports: [FormsModule, FieldValidationDirective],
  templateUrl: './consolidation.page.html',
  styleUrls: ['../../shared/styles/feature-page.css', './consolidation.page.css'],
})
export class ConsolidationPageComponent implements OnInit {
  readonly notifications = inject(NotificationService);
  groups: ConsolidationGroup[] = [];
  companies: ConsolidationCompany[] = [];
  runs: ConsolidationRunSummary[] = [];
  selectedGroupId: number | null = null;
  selectedRunId: number | null = null;
  currentRun: ConsolidationRun | null = null;
  editing = false;
  saving = false;
  loading = false;
  error = '';
  currencies: { code: string; name: string }[] = [];
  code = '';
  name = '';
  reportingCurrencyCode = '';
  selectedCompanyIds: number[] = [];
  periodEnd = new Date().toISOString().slice(0, 10);
  periodStart = `${this.periodEnd.slice(0, 7)}-01`;
  translationRateInputs: Record<number, { closingRate: number | null; averageRate: number | null }> = {};
  eliminationLines: ConsolidationElimination[] = [];

  constructor(
    private readonly service: ConsolidationService,
    private readonly api: ApiService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loading = true;
    forkJoin({
      groups: this.service.getGroups(),
      companies: this.service.getCompanies(),
      currencies: this.api.get<{ code: string; name: string }[]>('/api/v1/currencies', false),
    }).subscribe({
      next: result => {
        this.groups = result.groups;
        this.companies = result.companies;
        this.currencies = result.currencies;
        this.reportingCurrencyCode = this.companies[0]?.functionalCurrencyCode ?? '';
        this.loading = false;
        if (this.groups.length) this.selectGroup(this.groups[0].id);
        else this.beginCreate();
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load consolidation setup.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  get selectedGroup(): ConsolidationGroup | undefined {
    return this.groups.find(group => group.id === this.selectedGroupId);
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: this.currentRun?.reportingCurrencyCode ?? this.reportingCurrencyCode ?? 'USD',
    }).format(amount);
  }

  getCompanyName(companyId: number): string {
    return this.selectedGroup?.companies.find(company => company.id === companyId)?.legalName ?? String(companyId);
  }

  toggleCompany(companyId: number, event: Event): void {
    const checked = (event.currentTarget as HTMLInputElement).checked;
    this.selectedCompanyIds = checked
      ? [...new Set([...this.selectedCompanyIds, companyId])]
      : this.selectedCompanyIds.filter(id => id !== companyId);
  }

  beginCreate(): void {
    this.editing = true;
    this.selectedGroupId = null;
    this.selectedRunId = null;
    this.currentRun = null;
    this.runs = [];
    this.code = '';
    this.name = '';
    this.selectedCompanyIds = [];
    this.reportingCurrencyCode = this.companies[0]?.functionalCurrencyCode ?? '';
    this.error = '';
  }

  editGroup(): void {
    const group = this.selectedGroup;
    if (!group || this.runs.length) return;
    this.editing = true;
    this.code = group.code;
    this.name = group.name;
    this.reportingCurrencyCode = group.reportingCurrencyCode;
    this.selectedCompanyIds = group.companies.map(company => company.id);
    this.error = '';
  }

  cancelEdit(): void {
    this.editing = false;
    if (this.selectedGroup) this.selectGroup(this.selectedGroup.id);
  }

  saveGroup(): void {
    const payload = {
      code: this.code.trim(),
      name: this.name.trim(),
      reportingCurrencyCode: this.reportingCurrencyCode,
      companyIds: this.selectedCompanyIds,
    };
    if (!payload.code || !payload.name || !payload.reportingCurrencyCode || payload.companyIds.length < 2) {
      this.error = 'Enter a code and name, choose a reporting currency, and select at least two companies.';
      return;
    }
    this.saving = true;
    this.error = '';
    const request = this.selectedGroupId
      ? this.service.updateGroup(this.selectedGroupId, payload)
      : this.service.createGroup(payload);
    const action = this.selectedGroupId ? 'updated' : 'created';
    request.subscribe({
      next: group => {
        this.groups = [
          ...this.groups.filter(item => item.id !== group.id),
          group,
        ].sort((a, b) => a.name.localeCompare(b.name));
        this.saving = false;
        this.editing = false;
        this.notifications.success(`Consolidation group ${group.name} ${action} successfully.`);
        this.selectGroup(group.id);
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not save this consolidation group.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  selectGroup(groupId: number | string): void {
    const id = Number(groupId);
    const group = this.groups.find(item => item.id === id);
    if (!group) return;
    this.selectedGroupId = id;
    this.selectedRunId = null;
    this.currentRun = null;
    this.editing = false;
    this.error = '';
    this.reportingCurrencyCode = group.reportingCurrencyCode;
    this.translationRateInputs = Object.fromEntries(group.companies.map(company => [
      company.id,
      {
        closingRate: company.functionalCurrencyCode === group.reportingCurrencyCode ? 1 : null,
        averageRate: company.functionalCurrencyCode === group.reportingCurrencyCode ? 1 : null,
      },
    ]));
    this.service.getRuns(id).subscribe({
      next: runs => {
        this.runs = runs;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load consolidation runs.'));
        this.changeDetector.markForCheck();
      },
    });
  }

  createRun(): void {
    const group = this.selectedGroup;
    if (!group || !this.selectedGroupId ||
      !/^\d{4}-\d{2}-\d{2}$/.test(this.periodStart) ||
      !/^\d{4}-\d{2}-\d{2}$/.test(this.periodEnd) ||
      this.periodStart > this.periodEnd) {
      this.error = 'Choose a valid consolidation period start and end date.';
      return;
    }
    const translationRates: ConsolidationRateInput[] = group.companies.map(company => ({
      companyId: company.id,
      closingRate: Number(this.translationRateInputs[company.id]?.closingRate),
      averageRate: Number(this.translationRateInputs[company.id]?.averageRate),
    }));
    if (translationRates.some(rate => !Number.isFinite(rate.closingRate) || rate.closingRate <= 0 ||
      !Number.isFinite(rate.averageRate) || rate.averageRate <= 0)) {
      this.error = 'Enter positive closing and average exchange rates for every organization.';
      return;
    }
    this.saving = true;
    this.error = '';
    this.service.createRun(this.selectedGroupId, this.periodStart, this.periodEnd, translationRates).subscribe({
      next: run => {
        this.currentRun = run;
        this.eliminationLines = run.eliminations;
        this.selectedRunId = run.id;
        this.runs = [
          { id: run.id, periodEnd: run.periodEnd, status: run.status },
          ...this.runs.filter(item => item.id !== run.id),
        ].sort((a, b) => b.periodEnd.localeCompare(a.periodEnd));
        this.saving = false;
        this.notifications.success(`Consolidation run for ${run.periodStart}–${run.periodEnd} created.`);
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not create the consolidation run.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  selectRun(runId: number | string): void {
    const id = Number(runId);
    if (!this.selectedGroupId || !this.runs.some(run => run.id === id)) return;
    this.selectedRunId = id;
    this.currentRun = null;
    this.error = '';
    this.saving = true;
    this.service.getRun(this.selectedGroupId, id).subscribe({
      next: run => {
        this.currentRun = run;
        this.eliminationLines = run.eliminations;
        this.saving = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load the consolidation run.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  addEliminationLine(): void {
    this.eliminationLines = [...this.eliminationLines, {
      accountCode: '',
      accountName: '',
      debit: 0,
      credit: 0,
      description: null,
    }];
  }

  removeEliminationLine(index: number): void {
    this.eliminationLines = this.eliminationLines.filter((_, lineIndex) => lineIndex !== index);
  }

  saveEliminations(): void {
    if (!this.selectedGroupId || !this.currentRun || this.currentRun.status !== 'Draft') return;
    const lines = this.eliminationLines.filter(line =>
      line.accountCode.trim() || line.accountName.trim() || line.debit !== 0 || line.credit !== 0);
    if (lines.some(line => !line.accountCode.trim() || !line.accountName.trim() ||
      (Number(line.debit) > 0) === (Number(line.credit) > 0) ||
      !Number.isFinite(Number(line.debit)) || !Number.isFinite(Number(line.credit)))) {
      this.error = 'Each elimination line needs an account code and name and exactly one positive debit or credit.';
      return;
    }
    const debitTotal = lines.reduce((sum, line) => sum + Number(line.debit), 0);
    const creditTotal = lines.reduce((sum, line) => sum + Number(line.credit), 0);
    if (Math.abs(debitTotal - creditTotal) > 0.000001) {
      this.error = 'Intercompany elimination debits and credits must balance.';
      return;
    }
    this.saving = true;
    this.error = '';
    this.service.saveEliminations(this.selectedGroupId, this.currentRun.id, lines).subscribe({
      next: () => {
        this.notifications.success('Intercompany eliminations saved.');
        this.selectRun(this.currentRun!.id);
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not save intercompany eliminations.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  finalizeRun(): void {
    if (!this.selectedGroupId || !this.currentRun || this.currentRun.status !== 'Draft') return;
    this.saving = true;
    this.error = '';
    this.service.finalizeRun(this.selectedGroupId, this.currentRun.id).subscribe({
      next: () => {
        this.notifications.success('Consolidation run finalized.');
        this.selectRun(this.currentRun!.id);
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not finalize the consolidation run.'));
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}
