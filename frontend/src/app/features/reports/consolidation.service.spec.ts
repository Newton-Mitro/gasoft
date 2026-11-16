import { of } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ConsolidationService } from './consolidation.service';

describe('ConsolidationService', () => {
  const group = {
    id: 2,
    code: 'GROUP-1',
    name: 'Group 1',
    reportingCurrencyCode: 'USD',
    companies: [
      { id: 3, code: 'CO-1', legalName: 'Company One', functionalCurrencyCode: 'USD' },
      { id: 4, code: 'CO-2', legalName: 'Company Two', functionalCurrencyCode: 'USD' },
    ],
  };

  it('uses tenant-scoped endpoints for group, company, and run operations', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        if (endpoint.endsWith('/companies')) {
          return of(group.companies);
        }
        if (endpoint.endsWith('/runs')) return of([{ id: 8, periodEnd: '2026-10-31', status: 'Draft' }]);
        if (endpoint.endsWith('/runs/8')) {
          return of({
            id: 8,
            periodStart: '2026-10-01',
            periodEnd: '2026-10-31',
            status: 'Draft',
            reportingCurrencyCode: 'USD',
            accounts: [{ accountCode: '1000', accountName: 'Cash', debit: 100, credit: 0 }],
            translationRates: [
              { companyId: 3, sourceCurrencyCode: 'USD', targetCurrencyCode: 'USD', closingRate: 1, averageRate: 1 },
            ],
            eliminations: [],
            translationAdjustments: [],
          });
        }
        return of([group]);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return endpoint.endsWith('/runs')
          ? of({
              id: 8,
              periodStart: '2026-10-01',
              periodEnd: '2026-10-31',
              status: 'Draft',
              reportingCurrencyCode: 'USD',
              accounts: [{ accountCode: '1000', accountName: 'Cash', debit: 100, credit: 0 }],
              translationRates: [
                { companyId: 3, sourceCurrencyCode: 'USD', targetCurrencyCode: 'USD', closingRate: 1, averageRate: 1 },
              ],
              eliminations: [],
              translationAdjustments: [],
            })
          : of(group);
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of(group);
      },
    } as unknown as ApiService;
    const service = new ConsolidationService(api);

    service.getGroups().subscribe();
    service.getCompanies().subscribe();
    service.createGroup({
      code: 'GROUP-1',
      name: 'Group 1',
      reportingCurrencyCode: 'USD',
      companyIds: [3, 4],
    }).subscribe();
    service.updateGroup(2, {
      code: 'GROUP-1',
      name: 'Group 1',
      reportingCurrencyCode: 'USD',
      companyIds: [3, 4],
    }).subscribe();
    service.getRuns(2).subscribe();
    service.createRun(2, '2026-10-01', '2026-10-31', [
      { companyId: 3, closingRate: 1, averageRate: 1 },
      { companyId: 4, closingRate: 1.1, averageRate: 1.08 },
    ]).subscribe();
    service.getRun(2, 8).subscribe();
    service.saveEliminations(2, 8, [
      { accountCode: '2100', accountName: 'Intercompany payable', debit: 100, credit: 0, description: 'Eliminate balance' },
      { accountCode: '1100', accountName: 'Intercompany receivable', debit: 0, credit: 100, description: 'Eliminate balance' },
    ]).subscribe();
    service.finalizeRun(2, 8).subscribe();

    expect(calls.map(call => call.slice(0, 3))).toEqual([
      ['GET', '/api/v1/tenant/consolidation-groups', false],
      ['GET', '/api/v1/tenant/consolidation-groups/companies', false],
      ['POST', '/api/v1/tenant/consolidation-groups', {
        code: 'GROUP-1',
        name: 'Group 1',
        reportingCurrencyCode: 'USD',
        companyIds: [3, 4],
      }],
      ['PUT', '/api/v1/tenant/consolidation-groups/2', {
        code: 'GROUP-1',
        name: 'Group 1',
        reportingCurrencyCode: 'USD',
        companyIds: [3, 4],
      }],
      ['GET', '/api/v1/tenant/consolidation-groups/2/runs', false],
      ['POST', '/api/v1/tenant/consolidation-groups/2/runs', {
        periodStart: '2026-10-01',
        periodEnd: '2026-10-31',
        translationRates: [
          { companyId: 3, closingRate: 1, averageRate: 1 },
          { companyId: 4, closingRate: 1.1, averageRate: 1.08 },
        ],
      }],
      ['GET', '/api/v1/tenant/consolidation-groups/2/runs/8', false],
      ['PUT', '/api/v1/tenant/consolidation-groups/2/runs/8/eliminations', {
        lines: [
          { accountCode: '2100', accountName: 'Intercompany payable', debit: 100, credit: 0, description: 'Eliminate balance' },
          { accountCode: '1100', accountName: 'Intercompany receivable', debit: 0, credit: 100, description: 'Eliminate balance' },
        ],
      }],
      ['POST', '/api/v1/tenant/consolidation-groups/2/runs/8/finalize', {}],
    ]);
  });

  it('rejects malformed consolidation group data', () => {
    const api = ({
      get: () => of([{ ...group, companies: [{ id: 0 }] }]),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new ConsolidationService(api).getGroups().subscribe({
      error: error => receivedError = error,
    });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid records');
  });
});
