import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/api.service';
import { errorMessage } from '../../../shared/utils/page-utils';

interface NotificationEventPreference {
  eventType: string;
  category: string;
  name: string;
  description: string;
  inAppEnabled: boolean;
  emailEnabled: boolean;
}

interface NotificationPreferencesResponse {
  events: NotificationEventPreference[];
  emailConfigured: boolean;
  emailConfigurationSource: 'tenant' | 'global' | 'none';
}

interface NotificationEventGroup {
  category: string;
  events: NotificationEventPreference[];
}

@Component({
  selector: 'app-notification-settings-page',
  imports: [FormsModule],
  templateUrl: './notification-settings.page.html',
  styleUrl: './notification-settings.page.css',
})
export class NotificationSettingsPageComponent implements OnInit {
  private readonly categoryIcons: Record<string, string> = {
    Sales: 'fa-receipt',
    Purchases: 'fa-cart-shopping',
    Payments: 'fa-credit-card',
    Expenses: 'fa-wallet',
    Inventory: 'fa-boxes-stacked',
    Accounting: 'fa-book',
    'Fixed assets': 'fa-building',
    Banking: 'fa-building-columns',
    Other: 'fa-layer-group',
    Security: 'fa-shield-halved',
  };

  events: NotificationEventPreference[] = [];
  eventGroups: NotificationEventGroup[] = [];
  emailConfigured = false;
  emailConfigurationSource: NotificationPreferencesResponse['emailConfigurationSource'] = 'none';
  searchTerm = '';
  loading = false;
  saving = false;
  error = '';
  saved = false;
  private savedPreferences = '[]';

  get visibleEventGroups(): NotificationEventGroup[] {
    const searchTerm = this.searchTerm.trim().toLocaleLowerCase();
    return this.eventGroups
      .map(group => ({
        ...group,
        events: group.events.filter(event =>
          !searchTerm ||
          `${event.eventType} ${event.name} ${event.description}`.toLocaleLowerCase().includes(searchTerm),
        ),
      }))
      .filter(group => group.events.length > 0);
  }

  get visibleEventCount(): number {
    return this.visibleEventGroups.reduce((count, group) => count + group.events.length, 0);
  }

  get hasUnsavedChanges(): boolean {
    return this.currentPreferences() !== this.savedPreferences;
  }

  constructor(
    private readonly api: ApiService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.api.get<NotificationPreferencesResponse>('/api/v1/notifications/preferences', false).subscribe({
      next: result => {
        this.events = result.events;
        const groups = new Map<string, NotificationEventPreference[]>();
        for (const event of result.events) {
          const events = groups.get(event.category) ?? [];
          events.push(event);
          groups.set(event.category, events);
        }
        this.eventGroups = [...groups].map(([category, events]) => ({ category, events }));
        this.emailConfigured = result.emailConfigured;
        this.emailConfigurationSource = result.emailConfigurationSource;
        this.savedPreferences = this.currentPreferences();
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load notification settings.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  save(): void {
    if (this.saving || this.loading || !this.hasUnsavedChanges) return;
    this.saving = true;
    this.error = '';
    this.saved = false;
    this.api.put<void>(
      '/api/v1/notifications/preferences',
      { events: this.events.map(({ eventType, inAppEnabled, emailEnabled }) => ({
        eventType,
        inAppEnabled,
        emailEnabled,
      })) },
      false,
    ).subscribe({
      next: () => {
        this.savedPreferences = this.currentPreferences();
        this.saving = false;
        this.saved = true;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save notification settings.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  isGroupEnabled(group: NotificationEventGroup, channel: 'inAppEnabled' | 'emailEnabled'): boolean {
    return this.categoryEvents(group).every(event => event[channel]);
  }

  isGroupPartiallyEnabled(group: NotificationEventGroup, channel: 'inAppEnabled' | 'emailEnabled'): boolean {
    const events = this.categoryEvents(group);
    return events.some(event => event[channel]) && !this.isGroupEnabled(group, channel);
  }

  setGroupEnabled(
    group: NotificationEventGroup,
    channel: 'inAppEnabled' | 'emailEnabled',
    event: Event,
  ): void {
    if (!(event.target instanceof HTMLInputElement)) return;
    for (const preference of this.categoryEvents(group)) {
      preference[channel] = event.target.checked;
    }
    this.saved = false;
    this.changeDetector.markForCheck();
  }

  clearSearch(): void {
    this.searchTerm = '';
  }

  categoryIcon(category: string): string {
    return this.categoryIcons[category] ?? 'fa-layer-group';
  }

  private currentPreferences(): string {
    return JSON.stringify(this.events.map(({ eventType, inAppEnabled, emailEnabled }) => ({
      eventType,
      inAppEnabled,
      emailEnabled,
    })));
  }

  private categoryEvents(group: NotificationEventGroup): NotificationEventPreference[] {
    return this.eventGroups.find(item => item.category === group.category)?.events ?? group.events;
  }
}
