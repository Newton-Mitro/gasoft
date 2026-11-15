import { HttpParams } from '@angular/common/http';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { errorMessage } from '../../shared/utils/page-utils';
import { DataRow } from '../../shared/models/shared.models';

@Component({
  selector: 'app-dashboard-page',
  imports: [RouterLink],
  styleUrl: '../../shared/styles/feature-page.css',
  templateUrl: './dashboard.page.html',
})
export class DashboardPage implements OnInit {
  readonly loading = signal(true);
  readonly error = signal('');
  readonly summary = signal<DataRow>({});

  constructor(
    private readonly api: ApiService,
  ) {}

  ngOnInit(): void {
    forkJoin({
      trial: this.api.get<DataRow>('reports/trial-balance'),
      income: this.api.get<DataRow>('reports/income-statement'),
      accounts: this.api.get<DataRow>(
        'accounts/page',
        true,
        new HttpParams().set('page', '1').set('pageSize', '1'),
      ),
    }).subscribe({
      next: result => {
        this.summary.set({
          totalDebit: result.trial['totalDebit'],
          netIncome: result.income['netIncome'],
          totalAccounts: result.accounts['totalAccounts'],
          currencyCode: result.trial['currencyCode'],
        });
        this.loading.set(false);
      },
      error: error => {
        this.error.set(errorMessage(error, 'Could not load your workspace overview.'));
        this.loading.set(false);
      },
    });
  }

  get today(): string {
    return new Intl.DateTimeFormat(undefined, {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
    }).format(new Date());
  }

  displayValue(value: unknown): string {
    if (value === null || value === undefined || value === '') return '—';
    return String(value);
  }

  currency(value: unknown): string {
    const amount = Number(value);
    if (!Number.isFinite(amount)) return '—';
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: String(this.summary()['currencyCode'] ?? 'USD'),
      maximumFractionDigits: 2,
    }).format(amount);
  }
}
