import { PaginationComponent } from './pagination.component';

describe('PaginationComponent', () => {
  it('shows the first pages, nearby pages, and last pages with ellipses', () => {
    const component = new PaginationComponent();
    component.page = 7;
    component.totalPages = 13;

    expect(component.paginationItems).toEqual([
      1, 2, 3, 'ellipsis', 6, 7, 8, 'ellipsis', 12, 13,
    ]);
  });

  it('shows every page when the gaps are only one page wide', () => {
    const component = new PaginationComponent();
    component.page = 5;
    component.totalPages = 9;

    expect(component.paginationItems).toEqual([1, 2, 3, 4, 5, 6, 7, 8, 9]);
  });

  it('emits first, previous, selected, next, and last page changes', () => {
    const component = new PaginationComponent();
    component.page = 4;
    component.pageSize = 25;
    component.totalPages = 8;
    const changes: Array<{ page: number; pageSize: number }> = [];
    component.pageChange.subscribe(change => changes.push(change));

    component.goTo(1);
    component.goTo(3);
    component.goTo(6);
    component.goTo(8);

    expect(changes).toEqual([
      { page: 1, pageSize: 25 },
      { page: 3, pageSize: 25 },
      { page: 6, pageSize: 25 },
      { page: 8, pageSize: 25 },
    ]);
  });

  it('ignores navigation while a page request is loading', () => {
    const component = new PaginationComponent();
    component.page = 4;
    component.totalPages = 8;
    component.loading = true;
    const changes: Array<{ page: number; pageSize: number }> = [];
    component.pageChange.subscribe(change => changes.push(change));

    component.goTo(3);

    expect(changes).toEqual([]);
  });
});
