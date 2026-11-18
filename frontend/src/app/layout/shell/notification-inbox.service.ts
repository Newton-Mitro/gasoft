import { HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { PagedResult } from '../../shared/models/paged-result.model';
import { errorMessage } from '../../shared/utils/page-utils';

export interface AppNotification {
  id: number;
  title: string;
  message: string;
  category: string;
  link: string | null;
  createdAt: string;
  readAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class NotificationInboxService {
  readonly notifications = signal<AppNotification[]>([]);
  readonly unreadCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly hasMore = signal(false);
  private page = 1;

  constructor(private readonly api: ApiService) {}

  refreshCount(): void {
    this.api.get<{ count: number }>('/api/v1/notifications/unread-count', false).subscribe({
      next: result => {
        this.unreadCount.set(result.count);
        this.error.set('');
      },
      error: error => this.error.set(errorMessage(error, 'Could not refresh notification count.')),
    });
  }

  load(page = 1, append = false): void {
    this.loading.set(true);
    this.error.set('');
    const params = new HttpParams().set('page', page).set('pageSize', 20);
    forkJoin({
      page: this.api.get<PagedResult<AppNotification>>('/api/v1/notifications', false, params),
      count: this.api.get<{ count: number }>('/api/v1/notifications/unread-count', false),
    }).subscribe({
      next: result => {
        this.notifications.update(existing => append ? [...existing, ...result.page.items] : result.page.items);
        this.unreadCount.set(result.count.count);
        this.hasMore.set(result.page.page < result.page.totalPages);
        this.page = result.page.page;
        this.loading.set(false);
      },
      error: error => {
        this.error.set(errorMessage(error, 'Could not load notifications.'));
        this.loading.set(false);
      },
    });
  }

  loadMore(): void {
    if (!this.loading() && this.hasMore()) this.load(this.page + 1, true);
  }

  markRead(id: number): void {
    this.api.post<void>(`/api/v1/notifications/${id}/read`, {}, false).subscribe({
      next: () => this.load(),
      error: error => this.error.set(errorMessage(error, 'Could not mark the notification as read.')),
    });
  }

  dismiss(id: number): void {
    this.api.delete<void>(`/api/v1/notifications/${id}`, false).subscribe({
      next: () => this.load(),
      error: error => this.error.set(errorMessage(error, 'Could not dismiss the notification.')),
    });
  }
}
