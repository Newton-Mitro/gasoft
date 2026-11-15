import { Injectable } from '@angular/core';
import { Observable, catchError, defer, of, shareReplay, tap, throwError } from 'rxjs';
import { Choice } from '../models/shared.models';

interface CacheEntry {
  expiresAt: number;
  value?: Choice[];
  request?: Observable<Choice[]>;
}

@Injectable({ providedIn: 'root' })
export class OptionDataCacheService {
  private static readonly cacheDurationMs = 60_000;
  private readonly entries = new Map<string, CacheEntry>();

  get(key: string, load: () => Observable<Choice[]>): Observable<Choice[]> {
    const cached = this.entries.get(key);
    if (cached && cached.expiresAt > Date.now()) {
      return cached.value ? of(cached.value) : cached.request!;
    }

    const entry: CacheEntry = { expiresAt: Number.POSITIVE_INFINITY };
    const request = defer(load).pipe(
      tap(value => {
        entry.value = value;
        entry.expiresAt = Date.now() + OptionDataCacheService.cacheDurationMs;
      }),
      catchError(error => {
        if (this.entries.get(key) === entry) this.entries.delete(key);
        return throwError(() => error);
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    entry.request = request;
    this.entries.set(key, entry);
    return request;
  }

  clear(): void {
    this.entries.clear();
  }
}
