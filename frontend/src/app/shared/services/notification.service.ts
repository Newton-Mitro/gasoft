import { Injectable, inject } from '@angular/core';
import { HotToastService } from '@ngxpert/hot-toast';
import type { SweetAlertIcon } from 'sweetalert2';

interface ConfirmationOptions {
  title: string;
  text?: string;
  confirmButtonText?: string;
  cancelButtonText?: string;
  icon?: SweetAlertIcon;
  confirmButtonVariant?: 'primary' | 'danger' | 'warning';
}

interface PromptOptions {
  title: string;
  text: string;
  placeholder?: string;
  confirmButtonText?: string;
  requiredMessage?: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly toast = inject(HotToastService);

  private async loadSweetAlert() {
    const [, sweetAlert] = await Promise.all([
      import('sweetalert2/dist/sweetalert2.min.css'),
      import('sweetalert2/dist/sweetalert2.esm.all.js'),
    ]);
    return sweetAlert.default;
  }

  success(message: string): void {
    this.toast.success(message, { className: 'app-toast app-toast-success' });
  }

  error(message: string): void {
    if (!message.trim()) return;
    this.toast.error(message, {
      duration: 6000,
      className: 'app-toast app-toast-error',
    });
  }

  info(message: string): void {
    this.toast.info(message, { className: 'app-toast app-toast-info' });
  }

  successDialog(message: string): Promise<void> {
    return this.loadSweetAlert().then(Swal =>
      Swal.fire({
        title: 'Success',
        text: message,
        icon: 'success',
        confirmButtonText: 'Continue',
        buttonsStyling: false,
        customClass: {
          popup: 'app-swal-popup',
          title: 'app-swal-title',
          confirmButton: 'app-swal-confirm',
        },
      }).then(() => undefined));
  }

  confirm(options: ConfirmationOptions): Promise<boolean> {
    const confirmButtonVariant = options.confirmButtonVariant ?? 'primary';
    return this.loadSweetAlert().then(Swal => Swal.fire({
      title: options.title,
      text: options.text,
      icon: options.icon ?? 'warning',
      showCancelButton: true,
      confirmButtonText: options.confirmButtonText ?? 'Confirm',
      cancelButtonText: options.cancelButtonText ?? 'Cancel',
      reverseButtons: true,
      focusCancel: true,
      buttonsStyling: false,
      customClass: {
        popup: 'app-swal-popup',
        title: 'app-swal-title',
        confirmButton: `app-swal-confirm app-swal-confirm-${confirmButtonVariant}`,
        cancelButton: 'app-swal-cancel',
      },
    }).then(result => result.isConfirmed));
  }

  prompt(options: PromptOptions): Promise<string | null> {
    return this.loadSweetAlert().then(Swal => Swal.fire({
      title: options.title,
      input: 'textarea',
      inputLabel: options.text,
      inputPlaceholder: options.placeholder ?? '',
      inputAttributes: { 'aria-label': options.text },
      showCancelButton: true,
      confirmButtonText: options.confirmButtonText ?? 'Continue',
      cancelButtonText: 'Cancel',
      reverseButtons: true,
      focusCancel: true,
      buttonsStyling: false,
      customClass: {
        popup: 'app-swal-popup',
        title: 'app-swal-title',
        confirmButton: 'app-swal-confirm',
        cancelButton: 'app-swal-cancel',
      },
      inputValidator: value =>
        typeof value === 'string' && value.trim()
          ? undefined
          : options.requiredMessage ?? 'This field is required.',
    }).then(result => result.isConfirmed ? String(result.value).trim() : null));
  }
}
