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
    let requestCount = 0;
    const fail = () => {
      requestCount += 1;
      return throwError(() => new Error('request failed'));
    };

    cache.get('accounts', fail).subscribe({ error: () => undefined });
    cache.get('accounts', fail).subscribe({ error: () => undefined });

    expect(requestCount).toBe(2);
  });

  it('reloads data after invalidation', () => {
    const cache = new OptionDataCacheService();
    let requestCount = 0;
    const load = () => {
      requestCount += 1;
      return of([{ label: `Option ${requestCount}`, value: requestCount }]);
    };

    cache.get('journals', load).subscribe();
    cache.clear();
    cache.get('journals', load).subscribe();

    expect(requestCount).toBe(2);
  });
});
