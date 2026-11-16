import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { errorMessage } from '../../../shared/utils/page-utils';
import { TaxJurisdiction } from './tax-jurisdiction.model';
import { TaxJurisdictionService } from './tax-jurisdiction.service';

@Component({
  selector: 'app-tax-jurisdictions-page',
  templateUrl: './tax-jurisdictions.page.html',
  styleUrl: '../../../shared/styles/feature-page.css',
})
export class TaxJurisdictionsPageComponent implements OnInit {
  readonly title = 'Supported tax jurisdictions';
  readonly description = 'Country-level ISO jurisdictions available for company registrations and tax setup. Configure tax codes and rates separately.';
  rows: TaxJurisdiction[] = [];
  loading = false;
  error = '';
  private readonly countryNames = new Intl.DisplayNames(['en'], { type: 'region' });

  constructor(
    private readonly jurisdictions: TaxJurisdictionService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loading = true;
    this.jurisdictions.getAll().subscribe({
      next: rows => {
        this.rows = rows;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load supported tax jurisdictions.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  countryName(countryCode: string): string {
    return this.countryNames.of(countryCode) ?? countryCode;
  }
}
