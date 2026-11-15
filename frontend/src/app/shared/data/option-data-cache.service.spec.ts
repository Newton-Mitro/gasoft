import { Subject, of, throwError } from 'rxjs';
import { Choice } from '../models/shared.models';
import { OptionDataCacheService } from './option-data-cache.service';

describe('OptionDataCacheService', () => {
  it('shares in-flight option requests', () => {
    const cache = new OptionDataCacheService();
    let requestCount = 0;
    const response = new Subject<Choice[]>();
    const load = () => {
      requestCount += 1;
      return response;
    };

    cache.get('currencies', load).subscribe();
    cache.get('currencies', load).subscribe();
    response.next([{ label: 'USD · US Dollar', value: 'USD' }]);
    response.complete();

    expect(requestCount).toBe(1);
  });

  it('retries failed requests instead of keeping an error cached', () => {
    const cache = new OptionDataCacheService();
    const state = cache.state('accounts');
    let requestCount = 0;
    const fail = () => {
      requestCount += 1;
      return throwError(() => new Error('request failed'));
    };

    cache.get('accounts', fail).subscribe({ error: () => undefined });
    expect(state()).toBe('failed');
    cache.get('accounts', fail).subscribe({ error: () => undefined });

    expect(requestCount).toBe(2);
  });

  it('reloads data after invalidation', () => {
    const cache = new OptionDataCacheService();
    const state = cache.state('journals');
    let requestCount = 0;
    const load = () => {
      requestCount += 1;
      return of([{ label: `Option ${requestCount}`, value: requestCount }]);
    };

    cache.get('journals', load).subscribe();
    expect(state()).toBe('loaded');
    cache.clear();
    expect(state()).toBe('idle');
    cache.get('journals', load).subscribe();

    expect(requestCount).toBe(2);
  });

  it('marks requests as loading and successful requests as loaded', () => {
    const cache = new OptionDataCacheService();
    const state = cache.state('currencies');
    const response = new Subject<Choice[]>();

    cache.get('currencies', () => response).subscribe();
    expect(state()).toBe('loading');
    response.next([]);
    response.complete();
    expect(state()).toBe('loaded');
  });
});
