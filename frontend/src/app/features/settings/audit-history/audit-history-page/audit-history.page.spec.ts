import { HttpParams } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../../../../core/api.service';
import { AuditHistoryPageComponent } from './audit-history.page';

describe('AuditHistoryPageComponent', () => {
  let fixture: ComponentFixture<AuditHistoryPageComponent>;
  let api: { get: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    api = {
      get: vi.fn((_path: string, _companyScoped: boolean, params: HttpParams) => {
        const page = Number(params.get('page'));
        return of({
          items: [
            {
              id: page,
              action: 'Updated',
              entityType: 'ChartOfAccount',
              entityId: String(page),
              actorUserId: 1,
              actorName: 'Test user',
              detailsJson: '{}',
              createdAt: '2026-01-01T12:00:00Z',
            },
          ],
          page,
          pageSize: 1,
          totalCount: 2,
          totalPages: 2,
        });
      }),
    };

    TestBed.configureTestingModule({
      imports: [AuditHistoryPageComponent],
      providers: [{ provide: ApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(AuditHistoryPageComponent);
  });

  it('renders the paged audit results and requests the selected page', () => {
    fixture.detectChanges();

    expect(fixture.componentInstance.loading).toBe(false);
    expect(fixture.componentInstance.totalCount).toBe(2);
    expect(fixture.nativeElement.querySelector('.audit-table tbody tr td')?.textContent).not.toContain(
      'Loading',
    );
    expect(fixture.nativeElement.querySelector('app-pagination')).not.toBeNull();

    const nextPage = fixture.nativeElement.querySelector(
      'app-pagination button[aria-label="Next page"]',
    ) as HTMLButtonElement;
    nextPage.click();
    fixture.detectChanges();

    const requestParams = api.get.mock.calls.at(-1)?.[2] as HttpParams;
    expect(requestParams.get('page')).toBe('2');
    expect(fixture.componentInstance.records[0].id).toBe(2);
  });
});
