import { PagedResult } from '../models/paged-result.model';

export function parsePagedResult<T>(
  result: unknown,
  isItem: (value: unknown) => value is T,
  resourceName: string,
): PagedResult<T> {
  if (!result || typeof result !== 'object') {
    throw new Error(`The ${resourceName} response has an unsupported format.`);
  }
  const page = result as Record<string, unknown>;
  const items = page['items'];
  if (
    !Array.isArray(items) ||
    !items.every(isItem) ||
    !Number.isSafeInteger(page['page']) ||
    !Number.isSafeInteger(page['pageSize']) ||
    !Number.isSafeInteger(page['totalCount']) ||
    !Number.isSafeInteger(page['totalPages'])
  ) {
    throw new Error(`The ${resourceName} response contains invalid page data.`);
  }
  return {
    items,
    page: Number(page['page']),
    pageSize: Number(page['pageSize']),
    totalCount: Number(page['totalCount']),
    totalPages: Number(page['totalPages']),
  };
}
