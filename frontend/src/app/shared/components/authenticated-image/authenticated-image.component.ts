import { HttpClient } from '@angular/common/http';
import { Component, Input, OnChanges, OnDestroy, signal } from '@angular/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-authenticated-image',
  template: `
    @if (objectUrl()) {
      <img [src]="objectUrl()" [alt]="alt" />
    } @else if (imageLoadFailed()) {
      <span class="image-fallback" role="img" [attr.aria-label]="alt ? alt + ' could not be loaded' : 'Image could not be loaded'" [title]="alt ? alt + ' could not be loaded' : 'Image could not be loaded'">{{ fallback }}</span>
    } @else {
      <span class="image-fallback" aria-hidden="true">{{ fallback }}</span>
    }
  `,
  styles: [`
    :host { display: contents; }
    img { display: block; width: 100%; height: 100%; max-width: 100%; border-radius: inherit; object-fit: cover; }
    .image-fallback { display: grid; width: 100%; height: 100%; place-items: center; }
  `],
})
export class AuthenticatedImageComponent implements OnChanges, OnDestroy {
  @Input({ required: true }) src = '';
  @Input() alt = '';
  @Input() fallback = '';

  readonly objectUrl = signal<string | null>(null);
  readonly imageLoadFailed = signal(false);
  private request?: Subscription;

  constructor(private readonly http: HttpClient) {}

  ngOnChanges(): void {
    this.request?.unsubscribe();
    this.releaseObjectUrl();
    this.imageLoadFailed.set(false);
    if (!this.src) return;

    this.request = this.http.get(this.src, { responseType: 'blob' }).subscribe({
      next: image => this.objectUrl.set(URL.createObjectURL(image)),
      error: () => this.imageLoadFailed.set(true),
    });
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
    this.releaseObjectUrl();
  }

  private releaseObjectUrl(): void {
    const url = this.objectUrl();
    if (url) URL.revokeObjectURL(url);
    this.objectUrl.set(null);
  }
}
