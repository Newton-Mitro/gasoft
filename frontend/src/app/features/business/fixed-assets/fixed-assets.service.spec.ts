import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { FixedAssetsService } from './fixed-assets.service';

describe('FixedAssetsService', () => {
  const setup = {
    currencyCode: 'USD',
    assets: [],
    periods: [{ id: 5, startsOn: '2026-01-01', endsOn: '2026-01-31', status: 'Open' }],
    journals: [{ id: 2, code: 'GEN', name: 'General' }],
    accounts: [{ accountId: 7, code: '1500', name: 'Equipment', accountType: 'Asset' }],
    depreciationHistory: [],
  };

  it('uses company-scoped endpoints for register and asset lifecycle actions', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(setup);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of({ id: 9 });
      },
    } as unknown as ApiService;
    const service = new FixedAssetsService(api);

    service.getAll().subscribe();
    service.create({
      assetNumber: 'FA-001',
      name: 'Equipment',
      description: null,
      acquisitionDate: '2026-01-15',
      inServiceDate: '2026-01-15',
      acquisitionCost: 1200,
      residualValue: 0,
      usefulLifeMonths: 60,
      accountingPeriodId: 5,
      journalId: 2,
      assetAccountId: 7,
      accumulatedDepreciationAccountId: 8,
      depreciationExpenseAccountId: 10,
      creditAccountId: 11,
    }).subscribe();
    service.depreciate(9, 5, 2).subscribe();
    service.dispose(9, {
      disposalDate: '2026-01-31',
      proceeds: 100,
      accountingPeriodId: 5,
      journalId: 2,
      cashAccountId: 7,
      gainAccountId: 12,
      lossAccountId: 13,
    }).subscribe();

    expect(calls).toEqual([
      ['GET', 'fixed-assets', true],
      ['POST', 'fixed-assets', expect.objectContaining({ assetNumber: 'FA-001' }), true],
      ['POST', 'fixed-assets/9/depreciate', { throughAccountingPeriodId: 5, journalId: 2 }, true],
      ['POST', 'fixed-assets/9/dispose', {
        disposalDate: '2026-01-31',
        proceeds: 100,
        accountingPeriodId: 5,
        journalId: 2,
        cashAccountId: 7,
        gainAccountId: 12,
        lossAccountId: 13,
      }, true],
    ]);
  });

  it('rejects malformed asset register data', () => {
    const api = ({
      get: () => of({ ...setup, accounts: [{ accountId: 0 }] }),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new FixedAssetsService(api).getAll().subscribe({
      error: error => receivedError = error,
    });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid setup data');
  });
});
