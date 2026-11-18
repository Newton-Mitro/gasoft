import { DatePipe } from '@angular/common';
import { Component, HostListener, OnDestroy } from '@angular/core';
import { interval, Subscription } from 'rxjs';
import { NotificationInboxService } from './notification-inbox.service';

@Component({
  selector: 'app-notification-inbox',
  imports: [DatePipe],
  templateUrl: './notification-inbox.component.html',
  styleUrl: './notification-inbox.component.css',
})
export class NotificationInboxComponent implements OnDestroy {
  open = false;
  private readonly refresh: Subscription;

  constructor(readonly inbox: NotificationInboxService) {
    inbox.refreshCount();
    this.refresh = interval(60_000).subscribe(() => inbox.refreshCount());
  }

  ngOnDestroy(): void {
    this.refresh.unsubscribe();
  }

  toggle(): void {
    this.open = !this.open;
    if (this.open) this.inbox.load(1);
  }

  @HostListener('document:keydown.escape')
  close(): void {
    this.open = false;
  }
}
