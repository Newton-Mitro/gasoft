import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { errorMessage } from '../../../../shared/utils/page-utils';
import { PageChange, PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { TaxJurisdiction } from '../tax-jurisdiction.model';
import { TaxJurisdictionService } from '../tax-jurisdiction.service';
import { NotificationService } from '../../../../shared/services/notification.service';

@Component({
  selector: 'app-tax-jurisdictions-page',
  imports: [PaginationComponent],
  templateUrl: './tax-jurisdictions.page.html',
  styleUrl: '../../../../shared/styles/feature-page.css',
})
export class TaxJurisdictionsPageComponent implements OnInit {
  readonly notifications = inject(NotificationService);
  readonly title = 'Supported tax jurisdictions';
  readonly description = 'Country-level ISO jurisdictions available for company registrations and tax setup. Configure tax codes and rates separately.';
  rows: TaxJurisdiction[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 25;
  totalPages = 0;
  loading = false;
  error = '';
  private readonly countryNames = new Intl.DisplayNames(['en'], { type: 'region' });

  constructor(
    private readonly jurisdictions: TaxJurisdictionService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadRows();
  }

  onPageChange(change: PageChange): void {
    this.page = change.page;
    this.pageSize = change.pageSize;
    this.loadRows();
  }

  private loadRows(): void {
    this.loading = true;
    this.jurisdictions.getPage(this.page, this.pageSize).subscribe({
      next: result => {
        this.rows = result.items;
        this.page = result.page;
        this.pageSize = result.pageSize;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = '';
        this.notifications.error(errorMessage(error, 'Could not load supported tax jurisdictions.'));
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  countryName(countryCode: string): string {
    return this.countryNames.of(countryCode) ?? countryCode;
  }
}
