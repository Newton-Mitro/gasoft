import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Choice, DataRow } from '../models/shared.models';
import { OptionDataCacheService } from './option-data-cache.service';

interface OptionRequest {
  endpoint: string;
  global: boolean;
}

const OPTION_REQUESTS: Record<string, OptionRequest> = {
  groups: { endpoint: 'account-groups', global: false },
  years: { endpoint: 'fiscal-years', global: false },
  parties: { endpoint: '/api/v1/parties/', global: true },
  accounts: { endpoint: 'accounts', global: false },
  journals: { endpoint: 'journals', global: false },
  periods: { endpoint: 'accounting-periods', global: false },
  categories: { endpoint: '/api/v1/item-categories', global: true },
  currencies: { endpoint: '/api/v1/currencies', global: true },
  warehouses: { endpoint: 'warehouses', global: false },
  items: { endpoint: '/api/v1/items', global: true },
  units: { endpoint: '/api/v1/units-of-measure', global: true },
  jurisdictions: { endpoint: '/api/v1/tax-reference', global: true },
  taxCodes: { endpoint: '/api/v1/tax-reference', global: true },
};

@Injectable({ providedIn: 'root' })
export class OptionDataService {
  constructor(
    private readonly api: ApiService,
    private readonly cache: OptionDataCacheService,
  ) {}

  getOptions(source: string): Observable<Choice[]> {
    const request = OPTION_REQUESTS[source];
    if (!request) throw new Error(`Unknown option source: ${source}`);
    return this.cache.get(source, () =>
      this.api.get<unknown>(request.endpoint, !request.global).pipe(
        map(result => this.toOptions(source, result)),
      ),
    );
  }

  getTaxReferenceRows(): Observable<DataRow[]> {
    return this.api.get<DataRow>('/api/v1/tax-reference', false).pipe(
      map(reference => {
        const codes = Array.isArray(reference['codes']) ? reference['codes'].filter(isDataRow) : [];
        return codes.flatMap(code => {
          const rates = Array.isArray(code['rates']) ? code['rates'].filter(isDataRow) : [];
          if (!rates.length) {
            return [{
              code: code['code'],
              taxType: code['taxType'],
              ratePercent: 'No rate configured',
              validFrom: '—',
              validTo: '—',
            }];
          }
          return rates.map(rate => ({
            code: code['code'],
            taxType: code['taxType'],
            ratePercent: rate['ratePercent'],
            validFrom: rate['validFrom'],
            validTo: rate['validTo'] ?? '—',
          }));
        });
      }),
    );
  }

  toRows(result: unknown): DataRow[] {
    if (Array.isArray(result)) return result.filter(isDataRow);
    if (!result || typeof result !== 'object') return [];
    const response = result as DataRow;
    if (Array.isArray(response['items'])) return response['items'].filter(isDataRow);
    if (Array.isArray(response['accounts'])) return response['accounts'].filter(isDataRow);
    return [response];
  }

  private toOptions(source: string, result: unknown): Choice[] {
    if ((source === 'jurisdictions' || source === 'taxCodes') && result && typeof result === 'object') {
      if (source === 'taxCodes') {
        const codes = (result as DataRow)['codes'];
        if (Array.isArray(codes)) {
          return codes.filter(isDataRow).map(row => ({
            label: `${row['code']} · ${row['taxType']}`,
            value: Number(row['id']),
          }));
        }
      }
      const jurisdictions = (result as DataRow)['jurisdictions'];
      if (Array.isArray(jurisdictions)) {
        return jurisdictions.filter(isDataRow).map(row => ({
          label: `${row['code']} · ${row['countryCode']}`,
          value: Number(row['id']),
        }));
      }
    }

    const rows = this.toRows(result);
    if (source === 'accounts') {
      return rows
        .filter(row =>
          row['isActive'] === true &&
          row['allowPosting'] === true &&
          typeof row['code'] === 'string' &&
          typeof row['name'] === 'string')
        .map(row => ({
          label: `${row['code']} · ${row['name']}`,
          value: Number(row['id']),
        }))
        .filter(option => Number.isSafeInteger(option.value) && option.value > 0);
    }

    return rows.map(row => {
      if (source === 'currencies') return { label: `${row['code']} · ${row['name']}`, value: String(row['code']) };
      if (source === 'parties') return { label: `${row['partyNo']} · ${row['displayName']}`, value: Number(row['id']) };
      if (source === 'groups') return { label: `${row['code']} · ${row['name']}`, value: Number(row['id']) };
      if (source === 'years') return { label: `${row['startsOn']} – ${row['endsOn']}`, value: Number(row['id']) };
      if (source === 'periods') {
        return {
          label: `${formatDate(row['startsOn'])} – ${formatDate(row['endsOn'])} · ${row['status']}`,
          value: Number(row['id']),
        };
      }
      if (source === 'warehouses') return { label: `${row['code']} · ${row['name']}`, value: Number(row['id']) };
      if (source === 'items') return { label: `${row['sku']} · ${row['name']}`, value: Number(row['id']) };
      if (source === 'units') return { label: `${row['code']} · ${row['name']}`, value: String(row['code']) };
      return { label: `${row['code']} · ${row['name']}`, value: Number(row['id']) };
    });
  }
}

function isDataRow(item: unknown): item is DataRow {
  return Boolean(item && typeof item === 'object' && !Array.isArray(item));
}

function formatDate(value: unknown): string {
  if (typeof value !== 'string') return 'Date unavailable';
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  if (!match) return value;
  const [, year, month, day] = match;
  return new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
    .format(new Date(Number(year), Number(month) - 1, Number(day)));
}
