import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { FormValidationService } from '../../../../shared/services/form-validation.service';
import { NotificationService } from '../../../../shared/services/notification.service';
import { TenantUserService } from '../user.service';
import { UsersPageComponent } from './users.page';

describe('UsersPageComponent validation', () => {
  function createPage(serviceOverrides: Partial<TenantUserService> = {}) {
    TestBed.configureTestingModule({
      providers: [
        FormValidationService,
        { provide: NotificationService, useValue: { success: vi.fn(), error: vi.fn() } },
      ],
    });
    const service = {
      create: vi.fn(() => of({})),
      addExisting: vi.fn(() => of({})),
      ...serviceOverrides,
    } as unknown as TenantUserService;
    const page = TestBed.runInInjectionContext(
      () =>
        new UsersPageComponent(
          {} as ActivatedRoute,
          { navigateByUrl: vi.fn() } as unknown as Router,
          service,
          { markForCheck: vi.fn() } as unknown as ChangeDetectorRef,
        ),
    );
    page.view = 'create';
    return { page, service, validation: TestBed.inject(FormValidationService) };
  }

  it('shows inline errors for missing new-user details without submitting', () => {
    const { page, service, validation } = createPage();

    page.submit();

    expect(service.create).not.toHaveBeenCalled();
    expect(validation.messagesFor('displayName')).toEqual(['Enter a full name.']);
    expect(validation.messagesFor('email')).toEqual(['Enter a valid email address.']);
    expect(validation.messagesFor('role')).toEqual(['Select a workspace role.']);
    expect(validation.messagesFor('temporaryPassword')).toEqual(['Enter a temporary password.']);
  });

  it('shows inline errors for missing existing-user email and role', () => {
    const { page, service, validation } = createPage();
    page.setAccountMode('existing');

    page.submit();

    expect(service.addExisting).not.toHaveBeenCalled();
    expect(validation.messagesFor('email')).toEqual(['Enter a valid email address.']);
    expect(validation.messagesFor('role')).toEqual(['Select a workspace role.']);
  });

  it('maps server validation errors to the matching field', () => {
    const apiError = new HttpErrorResponse({
      status: 400,
      error: { errors: { Email: ['This email is already registered.'] } },
    });
    const { page, validation } = createPage({
      create: vi.fn(() => throwError(() => apiError)),
    });
    page.form = {
      displayName: 'Alex Morgan',
      email: 'alex@example.com',
      role: 'Member',
      temporaryPassword: 'temporary',
    };

    page.submit();

    expect(validation.messagesFor('email')).toEqual(['This email is already registered.']);
  });
});
