import { Account } from './account.model';

export interface AccountTreeNode {
  account: Account;
  children: AccountTreeNode[];
}

export function buildAccountTree(accounts: Account[], accountGroupId: number): AccountTreeNode[] {
  const nodes = new Map<number, AccountTreeNode>();
  const roots: AccountTreeNode[] = [];

  for (const account of accounts.filter(item => item.accountGroupId === accountGroupId)) {
    nodes.set(account.id, { account, children: [] });
  }

  for (const node of nodes.values()) {
    const parent = node.account.parentId === null ? undefined : nodes.get(node.account.parentId);
    if (parent && parent.account.id !== node.account.id) parent.children.push(node);
    else roots.push(node);
  }

  for (const node of nodes.values()) {
    node.children.sort(compareAccounts);
  }
  roots.sort(compareAccounts);
  return roots;
}

export function filterAccountTree(nodes: AccountTreeNode[], search: string): AccountTreeNode[] {
  const normalizedSearch = search.trim().toLocaleLowerCase();
  if (!normalizedSearch) return nodes;

  return nodes.flatMap(node => {
    const children = filterAccountTree(node.children, normalizedSearch);
    const matches = `${node.account.code} ${node.account.name}`.toLocaleLowerCase().includes(normalizedSearch);
    return matches || children.length ? [{ ...node, children }] : [];
  });
}

export function accountDescendantIds(accounts: Account[], accountId: number): Set<number> {
  const descendants = new Set<number>();
  let parentIds = [accountId];

  while (parentIds.length) {
    const children = accounts
      .filter(account => account.parentId !== null && parentIds.includes(account.parentId))
      .map(account => account.id)
      .filter(id => !descendants.has(id));
    for (const childId of children) descendants.add(childId);
    parentIds = children;
  }

  return descendants;
}

function compareAccounts(left: AccountTreeNode, right: AccountTreeNode): number {
  return left.account.code.localeCompare(right.account.code, undefined, { numeric: true }) ||
    left.account.name.localeCompare(right.account.name);
}
