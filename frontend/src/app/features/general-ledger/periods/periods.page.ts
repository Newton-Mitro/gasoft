import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { Choice, Field, RecordFormMode } from '../../../shared/models/shared.models';
import { OptionDataService } from '../../../shared/data/option-data.service';
import { AccountingPeriod, AccountingPeriodPayload } from './period.model';
import { AccountingPeriodService } from './period.service';

@Component({
  selector: 'app-periods-page',
  imports: [RouterLink, RecordFormComponent],
  templateUrl: './periods.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class PeriodsPageComponent implements OnInit, OnDestroy {
  readonly pageKey = 'periods';
  readonly title = 'Accounting periods';
  readonly description = 'Manage the posting windows within each fiscal year.';
  readonly fields: Field[] = [
    { key: 'fiscalYearId', label: 'Fiscal year', type: 'select', required: true, optionSource: 'years' },
    { key: 'startsOn', label: 'Starts on', type: 'date', required: true },
    { key: 'endsOn', label: 'Ends on', type: 'date', required: true },
  ];
  readonly indexPath = '/general-ledger/periods';
  rows: AccountingPeriod[] = [];
  form: Record<string, unknown> = {};
  options: Record<string, Choice[]> = {};
  loading = false;
  saving = false;
  error = '';
  view: 'index' | 'create' | 'edit' = 'index';
  private routeSubscription?: Subscription;
  private currentRecordId: number | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly periods: AccountingPeriodService,
    private readonly optionData: OptionDataService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = combineLatest([this.route.data, this.route.paramMap]).subscribe(([data, params]) => {
      const view = data['view'];
      this.view = view === 'create' || view === 'edit' ? view : 'index';
      this.currentRecordId = toRecordId(params);
      this.error = '';
      if (this.view === 'create') {
        this.form = {};
        this.loadYearOptions();
      } else if (this.view === 'edit') {
        this.loadYearOptions();
        this.loadRecord();
      } else {
        this.loadRows();
      }
    });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
  }

  get formMode(): RecordFormMode {
    return this.view === 'edit' ? 'edit' : 'create';
  }

  setFormValue(key: string, value: unknown): void {
    this.form[key] = value;
  }

  submit(): void {
    const payload: AccountingPeriodPayload = {
      fiscalYearId: Number(this.form['fiscalYearId']),
      startsOn: String(this.form['startsOn'] ?? ''),
      endsOn: String(this.form['endsOn'] ?? ''),
    };
    let request;
    if (this.view === 'edit') {
      if (this.currentRecordId === null) {
        this.error = 'A valid accounting period ID is required to update this record.';
        return;
      }
      request = this.periods.update(this.currentRecordId, payload);
    } else {
      request = this.periods.create(payload);
    }

    this.saving = true;
    this.error = '';
    request.subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save this accounting period.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  private loadYearOptions(): void {
    this.optionData.getOptions('years').subscribe({
      next: options => {
        this.options = { ...this.options, years: options };
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load fiscal year options.');
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRows(): void {
    this.loading = true;
    this.periods.getAll().subscribe({
      next: rows => {
        this.rows = rows;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load accounting periods.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    if (this.currentRecordId === null) {
      this.error = 'A valid accounting period ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.periods.getById(this.currentRecordId).subscribe({
      next: period => {
        this.form = {
          fiscalYearId: period.fiscalYearId,
          startsOn: period.startsOn,
          endsOn: period.endsOn,
        };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load this accounting period.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}

function toRecordId(params: ParamMap): number | null {
  const id = Number(params.get('recordId'));
  return Number.isSafeInteger(id) && id > 0 ? id : null;
}
