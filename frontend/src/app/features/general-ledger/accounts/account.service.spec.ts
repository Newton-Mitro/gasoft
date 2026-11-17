import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { Account, AccountPage, AccountPayload } from './account.model';
import { AccountService } from './account.service';

describe('AccountService', () => {
  it('loads all accounts for the tree view', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push([endpoint, companyScoped]);
        return of([{
          id: 2,
          companyId: 3,
          accountGroupId: 4,
          parentId: null,
          code: '1000',
          name: 'Cash',
          allowPosting: true,
          isActive: true,
        }]);
      },
    } as unknown as ApiService;
    let response: Account[] | undefined;

    new AccountService(api).getAll().subscribe(result => response = result);

    expect(calls).toEqual([['accounts', true]]);
    expect(response?.[0]?.code).toBe('1000');
  });

  it('loads the paginated, searchable account endpoint', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean, params: { toString(): string }) => {
        calls.push([endpoint, companyScoped, params.toString()]);
        return of({
          items: [{
            id: 2,
            companyId: 3,
            accountGroupId: 4,
            parentId: null,
            code: '1000',
            name: 'Cash',
            allowPosting: true,
            isActive: true,
          }],
          page: 1,
          pageSize: 25,
          totalCount: 1,
          totalPages: 1,
          totalAccounts: 1,
        });
      },
    } as unknown as ApiService;
    let response: AccountPage | undefined;

    new AccountService(api).getPage(' cash ', 1, 25).subscribe(result => response = result);

    expect(calls).toEqual([['accounts/page', true, 'page=1&pageSize=25&search=cash']]);
    expect(response?.items[0]?.code).toBe('1000');
  });

  it('sends the parent posting change with the reparent request', () => {
    const calls: unknown[][] = [];
    const api = {
      put: (endpoint: string, payload: AccountPayload, companyScoped: boolean) => {
        calls.push([endpoint, payload, companyScoped]);
        return of({
          id: 2,
          companyId: 3,
          accountGroupId: 4,
          parentId: 1,
          code: '1010',
          name: 'Bank',
          allowPosting: true,
          isActive: true,
        });
      },
    } as unknown as ApiService;
    const payload: AccountPayload = {
      accountGroupId: 4,
      code: '1010',
      name: 'Bank',
      parentId: 1,
      allowPosting: true,
      isActive: true,
      disableParentPosting: true,
    };

    new AccountService(api).update(2, payload).subscribe();

    expect(calls).toEqual([['accounts/2', payload, true]]);
  });

  it('rejects malformed account results', () => {
    const api = ({
      get: () => of({ items: [{ id: 2 }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1, totalAccounts: 1 }),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new AccountService(api).getPage('', 1, 25).subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid page data');
  });
});
