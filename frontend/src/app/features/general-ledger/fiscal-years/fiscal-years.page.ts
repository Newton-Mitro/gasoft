import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { errorMessage } from '../../../shared/utils/page-utils';
import { RecordFormComponent } from '../../../shared/components/record-form/record-form.component';
import { Field, RecordFormMode } from '../../../shared/models/shared.models';
import { FiscalYear, FiscalYearPayload } from './fiscal-year.model';
import { FiscalYearService } from './fiscal-year.service';

@Component({
  selector: 'app-fiscal-years-page',
  imports: [RouterLink, RecordFormComponent],
  templateUrl: './fiscal-years.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class FiscalYearsPageComponent implements OnInit, OnDestroy {
  readonly pageKey = 'fiscal-years';
  readonly title = 'Fiscal years';
  readonly description = 'Review the financial year calendar used by your company.';
  readonly fields: Field[] = [
    { key: 'startsOn', label: 'Starts on', type: 'date', required: true },
    { key: 'endsOn', label: 'Ends on', type: 'date', required: true },
  ];
  readonly indexPath = '/general-ledger/fiscal-years';
  rows: FiscalYear[] = [];
  form: Record<string, unknown> = {};
  loading = false;
  saving = false;
  error = '';
  success = '';
  view: 'index' | 'create' | 'edit' = 'index';
  private routeSubscription?: Subscription;
  private currentRecordId: number | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly fiscalYears: FiscalYearService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = combineLatest([this.route.data, this.route.paramMap]).subscribe(([data, params]) => {
      const view = data['view'];
      this.view = view === 'create' || view === 'edit' ? view : 'index';
      this.currentRecordId = toRecordId(params);
      this.error = '';
      this.success = '';
      if (this.view === 'create') {
        this.form = {};
        return;
      }
      if (this.view === 'edit') {
        this.loadRecord();
        return;
      }
      this.loadRows();
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
    const payload: FiscalYearPayload = {
      startsOn: String(this.form['startsOn'] ?? ''),
      endsOn: String(this.form['endsOn'] ?? ''),
    };
    this.saving = true;
    this.error = '';
    let request;
    if (this.view === 'edit') {
      if (this.currentRecordId === null) {
        this.error = 'A valid fiscal year ID is required to update this record.';
        this.saving = false;
        return;
      }
      request = this.fiscalYears.update(this.currentRecordId, payload);
    } else {
      request = this.fiscalYears.create(payload);
    }
    request.subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigateByUrl(this.indexPath);
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save this fiscal year.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  closeForm(): void {
    void this.router.navigateByUrl(this.indexPath);
  }

  private loadRows(): void {
    this.loading = true;
    this.fiscalYears.getAll().subscribe({
      next: rows => {
        this.rows = rows;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load fiscal years.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadRecord(): void {
    const id = this.currentRecordId;
    if (id === null) {
      this.error = 'A valid fiscal year ID is required to edit this record.';
      return;
    }
    this.loading = true;
    this.fiscalYears.getById(id).subscribe({
      next: year => {
        this.form = { startsOn: year.startsOn, endsOn: year.endsOn };
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load this fiscal year.');
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
