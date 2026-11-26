import { ChangeDetectorRef } from '@angular/core';
import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { NotificationSettingsPageComponent } from './notification-settings.page';

describe('NotificationSettingsPageComponent', () => {
  it('loads event channels and saves the selected in-app and email preferences', () => {
    const event = {
      eventType: 'record-posted',
      category: 'Other',
      name: 'Record posted',
      description: 'A record is posted.',
      inAppEnabled: true,
      emailEnabled: false,
    };
    const securityEvent = {
      eventType: 'password-changed',
      category: 'Security',
      name: 'Password changed',
      description: 'Your account password is changed.',
      inAppEnabled: true,
      emailEnabled: false,
    };
    const api = {
      get: vi.fn(() => of({
        events: [event, securityEvent],
        emailConfigured: true,
        emailConfigurationSource: 'tenant',
      })),
      put: vi.fn(() => of(undefined)),
    } as unknown as ApiService;
    const changeDetector = { markForCheck: vi.fn() } as unknown as ChangeDetectorRef;
    const page = new NotificationSettingsPageComponent(api, changeDetector);

    page.ngOnInit();
    expect(page.events).toEqual([event, securityEvent]);
    expect(page.eventGroups).toEqual([
      { category: 'Other', events: [event] },
      { category: 'Security', events: [securityEvent] },
    ]);
    expect(page.emailConfigured).toBe(true);
    expect(page.emailConfigurationSource).toBe('tenant');
    expect(page.hasUnsavedChanges).toBe(false);

    page.events[0].inAppEnabled = false;
    page.events[0].emailEnabled = true;
    expect(page.hasUnsavedChanges).toBe(true);
    page.searchTerm = 'password';
    expect(page.visibleEventGroups).toEqual([
      { category: 'Security', events: [securityEvent] },
    ]);
    const emailToggle = document.createElement('input');
    emailToggle.checked = true;
    const changeEvent = new Event('change');
    Object.defineProperty(changeEvent, 'target', { value: emailToggle });
    page.setGroupEnabled(page.visibleEventGroups[0], 'emailEnabled', changeEvent);
    expect(securityEvent.emailEnabled).toBe(true);
    page.save();

    expect(api.put).toHaveBeenCalledWith(
      '/api/v1/notifications/preferences',
      {
        events: [
          { eventType: 'record-posted', inAppEnabled: false, emailEnabled: true },
          { eventType: 'password-changed', inAppEnabled: true, emailEnabled: true },
        ],
      },
      false,
    );
    expect(page.saved).toBe(true);
    expect(page.saving).toBe(false);
    expect(page.hasUnsavedChanges).toBe(false);

    page.clearSearch();
    expect(page.searchTerm).toBe('');
    expect(page.visibleEventCount).toBe(2);
  });
});
