import { ChangeDetectorRef } from '@angular/core';
import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { EmailConfigurationPageComponent } from './email-configuration.page';

describe('EmailConfigurationPageComponent', () => {
  it('updates tenant SMTP settings without returning or resubmitting the saved password', () => {
    const api = {
      get: vi.fn(() => of({
        smtpHost: 'smtp.example.com',
        smtpPort: 587,
        smtpUsername: 'mailer',
        fromEmail: 'finance@example.com',
        fromName: 'Finance',
        hasPassword: true,
      })),
      put: vi.fn(() => of(undefined)),
    } as unknown as ApiService;
    const changeDetector = { markForCheck: vi.fn() } as unknown as ChangeDetectorRef;
    const page = new EmailConfigurationPageComponent(api, changeDetector);

    page.ngOnInit();
    expect(page.smtpHost).toBe('smtp.example.com');
    expect(page.smtpPassword).toBe('');
    expect(page.hasPassword).toBe(true);

    page.smtpHost = 'smtp2.example.com';
    page.save();

    expect(api.put).toHaveBeenCalledWith(
      '/api/v1/settings/email',
      {
        smtpHost: 'smtp2.example.com',
        smtpPort: 587,
        smtpUsername: 'mailer',
        smtpPassword: null,
        fromEmail: 'finance@example.com',
        fromName: 'Finance',
        clearPassword: false,
      },
      false,
    );
    expect(page.saved).toBe(true);
    expect(page.hasPassword).toBe(true);
  });
});
