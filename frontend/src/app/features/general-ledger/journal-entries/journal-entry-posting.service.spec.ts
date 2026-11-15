import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { JournalEntryPostingService } from './journal-entry-posting.service';

describe('JournalEntryPostingService', () => {
  it('posts journal entries through the company-scoped API', () => {
    const calls: unknown[][] = [];
    const api = {
      post: (endpoint: string, payload: unknown, companyScoped: boolean) => {
        calls.push([endpoint, payload, companyScoped]);
        return of({ id: 5, status: 'Posted' });
      },
    } as unknown as ApiService;

    new JournalEntryPostingService(api).postJournalEntry(5).subscribe();

    expect(calls).toEqual([['journal-entries/5/post', {}, true]]);
  });
});
