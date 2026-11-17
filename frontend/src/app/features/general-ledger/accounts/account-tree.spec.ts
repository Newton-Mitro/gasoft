import { Account } from './account.model';
import { accountDescendantIds, buildAccountTree, filterAccountTree } from './account-tree';

const accounts: Account[] = [
  { id: 1, companyId: 1, accountGroupId: 10, parentId: null, code: '1000', name: 'Cash', allowPosting: false, isActive: true },
  { id: 2, companyId: 1, accountGroupId: 10, parentId: 1, code: '1010', name: 'Bank', allowPosting: false, isActive: true },
  { id: 3, companyId: 1, accountGroupId: 10, parentId: 2, code: '1011', name: 'Checking', allowPosting: true, isActive: true },
  { id: 4, companyId: 1, accountGroupId: 20, parentId: null, code: '2000', name: 'Payables', allowPosting: true, isActive: true },
];

describe('account tree', () => {
  it('builds a sorted hierarchy scoped to the selected account group', () => {
    const tree = buildAccountTree(accounts, 10);

    expect(tree.map(node => node.account.code)).toEqual(['1000']);
    expect(tree[0]?.children[0]?.account.code).toBe('1010');
    expect(tree[0]?.children[0]?.children[0]?.account.code).toBe('1011');
  });

  it('keeps ancestors visible when a child matches the search', () => {
    const tree = filterAccountTree(buildAccountTree(accounts, 10), 'checking');

    expect(tree[0]?.account.name).toBe('Cash');
    expect(tree[0]?.children[0]?.account.name).toBe('Bank');
    expect(tree[0]?.children[0]?.children[0]?.account.name).toBe('Checking');
  });

  it('finds descendants to prevent dropping an account beneath itself', () => {
    expect(accountDescendantIds(accounts, 1)).toEqual(new Set([2, 3]));
  });
});
