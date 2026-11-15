import { Injectable, Signal, WritableSignal, signal } from '@angular/core';
import { Observable, catchError, defer, of, shareReplay, tap, throwError } from 'rxjs';

export type OptionLoadState = 'idle' | 'loading' | 'loaded' | 'failed';

interface CacheEntry<T> {
  expiresAt: number;
  hasValue: boolean;
  value?: T;
  request: Observable<T>;
}

@Injectable({ providedIn: 'root' })
export class OptionDataCacheService {
  private static readonly cacheDurationMs = 60_000;
  private readonly entries = new Map<string, CacheEntry<unknown>>();
  private readonly states = new Map<string, WritableSignal<OptionLoadState>>();

  get<T>(key: string, load: () => Observable<T>): Observable<T> {
    const cached = this.entries.get(key) as CacheEntry<T> | undefined;
    if (cached && cached.expiresAt > Date.now()) {
      return cached.hasValue ? of(cached.value as T) : cached.request;
    }

    const state = this.writableState(key);
    state.set('loading');
    let entry: CacheEntry<T>;
    const request = defer(load).pipe(
      tap(value => {
        entry.value = value;
        entry.hasValue = true;
        entry.expiresAt = Date.now() + OptionDataCacheService.cacheDurationMs;
        if (this.entries.get(key) === entry) state.set('loaded');
      }),
      catchError(error => {
        if (this.entries.get(key) === entry) {
          this.entries.delete(key);
          state.set('failed');
        }
        return throwError(() => error);
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    entry = { expiresAt: Number.POSITIVE_INFINITY, hasValue: false, request };
    this.entries.set(key, entry as CacheEntry<unknown>);
    return request;
  }

  state(key: string): Signal<OptionLoadState> {
    return this.writableState(key);
  }

  private writableState(key: string): WritableSignal<OptionLoadState> {
    let state = this.states.get(key);
    if (!state) {
      state = signal<OptionLoadState>('idle');
      this.states.set(key, state);
    }
    return state;
  }

  clear(): void {
    this.entries.clear();
    for (const state of this.states.values()) state.set('idle');
  }
}
