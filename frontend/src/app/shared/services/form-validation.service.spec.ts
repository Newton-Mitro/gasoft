import { FormValidationService } from './form-validation.service';

describe('FormValidationService', () => {
  it('matches server field names case-insensitively and clears a field after it changes', () => {
    const validation = new FormValidationService();
    validation.setErrors({
      'request.AccountGroupId': ['Choose a valid account group.'],
      name: ['Name is required.'],
    });

    expect(validation.messagesFor('accountGroupId')).toEqual(['Choose a valid account group.']);
    validation.clearField('Name');
    expect(validation.messagesFor('name')).toEqual([]);
    expect(validation.messagesFor('accountGroupId')).toEqual(['Choose a valid account group.']);
  });

  it('maps dynamic line input names to nested request fields', () => {
    const validation = new FormValidationService();
    validation.setErrors({
      'lines[0].accountId': ['Choose a valid account.'],
      'lines[1].unitCost': ['Enter a valid unit cost.'],
      'lines[2].taxCodeId': ['Choose a valid tax code.'],
    });

    expect(validation.messagesFor('lineAccount0')).toEqual(['Choose a valid account.']);
    expect(validation.messagesFor('unit-cost-1')).toEqual(['Enter a valid unit cost.']);
    expect(validation.messagesFor('tax-2')).toEqual(['Choose a valid tax code.']);
    validation.clearField('lineAccount0');
    expect(validation.messagesFor('lines[0].accountId')).toEqual([]);
  });

  it('maps custom form names to nested and aliased request fields', () => {
    const validation = new FormValidationService();
    validation.setErrors({
      'translationRates[0].closingRate': ['Enter a positive closing rate.'],
      SupplierId: ['Choose a supplier.'],
      ThroughAccountingPeriodId: ['Choose a period.'],
      'Lines[0].unitCost': ['Enter a valid unit cost.'],
      'TaxAccounts[1].accountId': ['Choose a tax account.'],
      Quantity: ['Enter a valid quantity.'],
    });

    expect(validation.messagesFor('translationRate0ClosingRate')).toEqual(['Enter a positive closing rate.']);
    expect(validation.messagesFor('supplier')).toEqual(['Choose a supplier.']);
    expect(validation.messagesFor('depreciationThroughPeriod')).toEqual(['Choose a period.']);
    expect(validation.messagesFor('unit-cost-0')).toEqual(['Enter a valid unit cost.']);
    expect(validation.messagesFor('taxAccount1AccountId')).toEqual(['Choose a tax account.']);
    expect(validation.messagesFor('receiveQuantity')).toEqual(['Enter a valid quantity.']);
  });

  it('only includes unregistered fields in the global fallback summary', () => {
    const validation = new FormValidationService();
    validation.setErrors({
      'customerId': ['Choose a customer.'],
      'lines[0].quantity': ['Enter a valid quantity.'],
    });
    const unregister = validation.registerField('customer');

    expect(validation.unmatchedErrors()).toEqual({
      'lines[0].quantity': ['Enter a valid quantity.'],
    });
    unregister();
    expect(validation.unmatchedErrors()).toEqual(validation.errors());
  });
});
