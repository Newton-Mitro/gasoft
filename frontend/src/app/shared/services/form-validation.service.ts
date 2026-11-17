import { Injectable, signal } from '@angular/core';

export type FormValidationErrors = Record<string, string[]>;

@Injectable({ providedIn: 'root' })
export class FormValidationService {
  private readonly currentErrors = signal<FormValidationErrors>({});
  private readonly registeredFields = signal(new Map<string, number>());
  readonly errors = this.currentErrors.asReadonly();

  unmatchedErrors(): FormValidationErrors {
    const registered = this.registeredFields();
    return Object.fromEntries(
      Object.entries(this.currentErrors()).filter(([field]) => !registered.has(normalizeField(field))),
    );
  }

  registerField(field: string): () => void {
    const key = normalizeField(field);
    this.registeredFields.update(fields => {
      const updated = new Map(fields);
      updated.set(key, (updated.get(key) ?? 0) + 1);
      return updated;
    });

    return () => this.registeredFields.update(fields => {
      const updated = new Map(fields);
      const count = updated.get(key) ?? 0;
      if (count <= 1) updated.delete(key);
      else updated.set(key, count - 1);
      return updated;
    });
  }

  setErrors(errors: FormValidationErrors): void {
    this.currentErrors.set(errors);
  }

  clear(): void {
    this.currentErrors.set({});
  }

  clearField(field: string): void {
    const key = Object.keys(this.currentErrors()).find(candidate => normalizeField(candidate) === normalizeField(field));
    if (!key) return;
    const remaining = { ...this.currentErrors() };
    delete remaining[key];
    this.currentErrors.set(remaining);
  }

  messagesFor(field: string): string[] {
    const normalized = normalizeField(field);
    return Object.entries(this.currentErrors())
      .filter(([key]) => normalizeField(key) === normalized)
      .flatMap(([, messages]) => messages);
  }
}

function normalizeField(field: string): string {
  const nestedItem = field.match(/(?:^|\.)(\w+)\[(\d+)\]\.(\w+)$/i);
  if (nestedItem) {
    return `${normalizeProperty(nestedItem[1])}${nestedItem[2]}${normalizeProperty(nestedItem[3])}`;
  }

  const normalized = normalizeProperty(field.split('.').at(-1) ?? field);
  const budgetLine = normalized.match(/^budgetline(\d+)amount$/);
  if (budgetLine) return `lines${budgetLine[1]}amount`;
  const dynamicLine = normalized.match(/^(?:journal|credit)?line(.+?)(\d+)$/);
  if (dynamicLine) return `lines${dynamicLine[2]}${normalizeLineProperty(dynamicLine[1])}`;
  const indexedLine = field.match(/^(item|description|quantity|unit[-_]?cost|price|account|tax)-(\d+)$/i);
  if (indexedLine) {
    return `lines${indexedLine[2]}${normalizeLineProperty(indexedLine[1])}`;
  }
  const elimination = field.match(/^elim-(code|name|description|debit|credit)-(\d+)$/i);
  if (elimination) {
    const property = elimination[1].toLowerCase() === 'code' ? 'accountcode' :
      elimination[1].toLowerCase() === 'name' ? 'accountname' : elimination[1].toLowerCase();
    return `lines${elimination[2]}${property}`;
  }
  const translationRate = normalized.match(/^translationrate(\d+)(closingrate|averagerate)$/);
  if (translationRate) return `translationrates${translationRate[1]}${translationRate[2]}`;
  const taxAccount = normalized.match(/^taxaccount(\d+)(accountid|taxcodeid)$/);
  if (taxAccount) return `taxaccounts${taxAccount[1]}${taxAccount[2]}`;
  if (/^allocation-\d+$/i.test(field)) return 'amount';

  const aliases: Record<string, string> = {
    account: 'accountid',
    taxcode: 'taxcodeid',
    customer: 'customerid',
    supplier: 'supplierid',
    currency: 'currencycode',
    lastfour: 'accountnumberlastfour',
    chartaccount: 'chartofaccountid',
    warehouse: 'warehouseid',
    reportingcurrency: 'reportingcurrencycode',
    fiscalyear: 'fiscalyearid',
    budget: 'budgetid',
    journal: 'journalid',
    period: 'accountingperiodid',
    invoice: 'salesinvoiceid',
    receivable: 'receivableaccountid',
    acquisitionperiod: 'accountingperiodid',
    acquisitionjournal: 'journalid',
    receivemovementnumber: 'movementnumber',
    receivedate: 'movementdate',
    receivequantity: 'quantity',
    receivejournal: 'journalid',
    receiveperiod: 'accountingperiodid',
    inventoryaccount: 'inventoryaccountid',
    assetaccount: 'assetaccountid',
    accumulatedaccount: 'accumulateddepreciationaccountid',
    creditaccount: 'creditaccountid',
    depreciationexpenseaccount: 'depreciationexpenseaccountid',
    depreciationthroughperiod: 'throughaccountingperiodid',
    depreciationjournal: 'journalid',
    disposalperiod: 'accountingperiodid',
    disposalcashaccount: 'cashaccountid',
    disposalgainaccount: 'gainaccountid',
    disposallossaccount: 'lossaccountid',
    disposalproceeds: 'proceeds',
    refundjournal: 'journalid',
    refundperiod: 'accountingperiodid',
    refundamount: 'amount',
    adjustmentdate: 'entrydate',
    adjustmentdescription: 'description',
    cashaccount: 'cashaccountid',
    offsetaccount: 'offsetaccountid',
    accountnumberlastfour: 'accountnumberlastfour',
    company: 'companyid',
    group: 'groupid',
    newbudgetname: 'name',
  };
  return aliases[normalized] ?? normalized;
}

function normalizeProperty(field: string): string {
  return field.replace(/[^a-z0-9]/gi, '').toLowerCase();
}

function normalizeLineProperty(field: string): string {
  const property = normalizeProperty(field);
  const aliases: Record<string, string> = {
    account: 'accountid',
    tax: 'taxcodeid',
    taxcode: 'taxcodeid',
    item: 'itemid',
    price: 'unitprice',
  };
  return aliases[property] ?? property;
}
