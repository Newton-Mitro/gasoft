import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/api.service';
import { errorMessage } from '../../../shared/utils/page-utils';

interface TenantEmailConfiguration {
  smtpHost: string;
  smtpPort: number;
  smtpUsername: string;
  fromEmail: string;
  fromName: string;
  hasPassword: boolean;
}

@Component({
  selector: 'app-email-configuration-page',
  imports: [FormsModule],
  templateUrl: './email-configuration.page.html',
  styleUrl: './email-configuration.page.css',
})
export class EmailConfigurationPageComponent implements OnInit {
  smtpHost = '';
  smtpPort = 587;
  smtpUsername = '';
  smtpPassword = '';
  fromEmail = '';
  fromName = 'Gasoft';
  hasPassword = false;
  clearPassword = false;
  loading = false;
  saving = false;
  error = '';
  saved = false;

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
    this.api.get<TenantEmailConfiguration>('/api/v1/settings/email', false).subscribe({
      next: result => {
        this.smtpHost = result.smtpHost;
        this.smtpPort = result.smtpPort;
        this.smtpUsername = result.smtpUsername;
        this.fromEmail = result.fromEmail;
        this.fromName = result.fromName;
        this.hasPassword = result.hasPassword;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not load email configuration.');
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  save(): void {
    if (this.saving || this.loading) return;
    this.saving = true;
    this.error = '';
    this.saved = false;
    this.api.put<void>(
      '/api/v1/settings/email',
      {
        smtpHost: this.smtpHost,
        smtpPort: this.smtpPort,
        smtpUsername: this.smtpUsername,
        smtpPassword: this.smtpPassword || null,
        fromEmail: this.fromEmail,
        fromName: this.fromName,
        clearPassword: this.clearPassword,
      },
      false,
    ).subscribe({
      next: () => {
        this.hasPassword = !!this.smtpPassword || (this.hasPassword && !this.clearPassword);
        this.smtpPassword = '';
        this.clearPassword = false;
        this.saving = false;
        this.saved = true;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = errorMessage(error, 'Could not save email configuration.');
        this.saving = false;
        this.changeDetector.markForCheck();
      },
    });
  }
}
