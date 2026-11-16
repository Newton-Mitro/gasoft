import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { AccountGroupPage } from './account-group.model';
import { AccountGroupService } from './account-group.service';

describe('AccountGroupService', () => {
  it('requests the paginated endpoint with search and validates its response', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean, params: { toString(): string }) => {
        calls.push([endpoint, companyScoped, params.toString()]);
        return of({
          items: [{
            id: 2,
            companyId: 3,
            code: 'ASSET',
            name: 'Assets',
            accountType: 'Asset',
            normalBalance: 'Debit',
          }],
          page: 1,
          pageSize: 25,
          totalCount: 1,
          totalPages: 1,
        });
      },
    } as unknown as ApiService;
    let response: AccountGroupPage | undefined;

    new AccountGroupService(api).getPage('assets', 1, 25).subscribe(result => response = result);

    expect(calls).toEqual([[
      'account-groups/page',
      true,
      'page=1&pageSize=25&search=assets',
    ]]);
    expect(response?.items[0]?.normalBalance).toBe('Debit');
  });

  it('rejects invalid page or item records', () => {
    const api = ({
      get: () => of({ items: [{ id: 2 }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 }),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new AccountGroupService(api).getPage('', 1, 25).subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid page data');
  });
});
