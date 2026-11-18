export interface FixedAsset {
  id: number;
  assetNumber: string;
  name: string;
  description: string | null;
  acquisitionDate: string;
  inServiceDate: string;
  acquisitionCost: number;
  residualValue: number;
  usefulLifeMonths: number;
  assetAccountId: number;
  accumulatedDepreciationAccountId: number;
  depreciationExpenseAccountId: number;
  acquisitionJournalEntryId: number;
  status: string;
  disposalDate: string | null;
  disposalProceeds: number | null;
  disposalJournalEntryId: number | null;
  accumulatedDepreciation: number;
  netBookValue: number;
  assetAccountName: string;
  accumulatedDepreciationAccountName: string;
  depreciationExpenseAccountName: string;
}

export interface FixedAssetPeriod {
  id: number;
  startsOn: string;
  endsOn: string;
  status: string;
}

export interface FixedAssetJournal {
  id: number;
  code: string;
  name: string;
}

export interface FixedAssetAccount {
  accountId: number;
  code: string;
  name: string;
  accountType: string;
}

export interface FixedAssetDepreciationRecord {
  assetId: number;
  depreciationDate: string;
  amount: number;
  journalEntryId: number;
}

export interface FixedAssetSetup {
  currencyCode: string;
  assets: FixedAsset[];
  periods: FixedAssetPeriod[];
  journals: FixedAssetJournal[];
  accounts: FixedAssetAccount[];
  depreciationHistory: FixedAssetDepreciationRecord[];
}

export interface PagedFixedAssetSetup extends Omit<FixedAssetSetup, 'assets'> {
  assets: PagedResult<FixedAsset>;
}

export interface FixedAssetCreatePayload {
  assetNumber: string;
  name: string;
  description: string | null;
  acquisitionDate: string;
  inServiceDate: string;
  acquisitionCost: number;
  residualValue: number;
  usefulLifeMonths: number;
  accountingPeriodId: number;
  journalId: number;
  assetAccountId: number;
  accumulatedDepreciationAccountId: number;
  depreciationExpenseAccountId: number;
  creditAccountId: number;
}
import { PagedResult } from '../../../shared/models/paged-result.model';
