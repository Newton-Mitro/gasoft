import { of } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { NotificationInboxService } from './notification-inbox.service';

describe('NotificationInboxService', () => {
  const first = {
    id: 1,
    title: 'Journal entry posted',
    message: 'A JournalEntry record was posted.',
    category: 'workflow',
    link: null,
    createdAt: '2026-01-02T03:04:05Z',
    readAt: null,
  };
  const second = { ...first, id: 2, title: 'Accounting period status changed' };

  it('loads inbox items and unread count, then appends older items', () => {
    const calls: string[] = [];
    const api = {
      get: (path: string, _companyScoped: boolean, params?: { get(name: string): string | null }) => {
        calls.push(`${path}?page=${params?.get('page') ?? ''}`);
        if (path.endsWith('/unread-count')) return of({ count: 1 });
        const page = Number(params?.get('page'));
        return of({
          items: page === 1 ? [first] : [second],
          page,
          pageSize: 20,
          totalCount: 2,
          totalPages: 2,
        });
      },
    } as unknown as ApiService;
    const inbox = new NotificationInboxService(api);

    inbox.load();

    expect(inbox.notifications()).toEqual([first]);
    expect(inbox.unreadCount()).toBe(1);
    expect(inbox.hasMore()).toBe(true);

    inbox.loadMore();

    expect(inbox.notifications()).toEqual([first, second]);
    expect(inbox.hasMore()).toBe(false);
    expect(calls).toContain('/api/v1/notifications?page=2');
  });

  it('marks read and dismiss actions through the current-user endpoints', () => {
    const calls: string[] = [];
    const api = {
      get: (path: string) => path.endsWith('/unread-count')
        ? of({ count: 0 })
        : of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }),
      post: (path: string) => {
        calls.push(`POST ${path}`);
        return of(undefined);
      },
      delete: (path: string) => {
        calls.push(`DELETE ${path}`);
        return of(undefined);
      },
    } as unknown as ApiService;
    const inbox = new NotificationInboxService(api);

    inbox.markRead(7);
    inbox.dismiss(8);

    expect(calls).toEqual([
      'POST /api/v1/notifications/7/read',
      'DELETE /api/v1/notifications/8',
    ]);
  });
});
