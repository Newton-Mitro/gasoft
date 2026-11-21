namespace Gasoft.Application.Billing;

public sealed record CreateInvoiceRequest(
    long PartyId,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string CurrencyCode,
    decimal ExchangeRate,
    IReadOnlyList<CreateDocumentLineRequest> Lines);
public sealed record UpdateInvoiceRequest(
    long PartyId,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string CurrencyCode,
    decimal ExchangeRate,
    IReadOnlyList<CreateDocumentLineRequest> Lines);

public sealed record CreateSalesCreditNoteRequest(
    long SalesInvoiceId,
    string CreditNoteNumber,
    DateOnly CreditNoteDate,
    string Reason,
    IReadOnlyList<CreateCreditNoteLineRequest> Lines);

public sealed record CreateCreditNoteLineRequest(long SalesInvoiceLineId, decimal Quantity);

public sealed record PostSalesCreditNoteRequest(
    long JournalId,
    long AccountingPeriodId,
    long ReceivableAccountId,
    IReadOnlyList<TaxAccountMapping> TaxAccounts);

public sealed record CreateSalesQuoteRequest(
    long CustomerId,
    string QuoteNumber,
    DateOnly QuoteDate,
    DateOnly ValidUntil,
    string CurrencyCode,
    decimal ExchangeRate,
    IReadOnlyList<CreateDocumentLineRequest> Lines);

public sealed record UpdateSalesQuoteRequest(
    long CustomerId,
    string QuoteNumber,
    DateOnly QuoteDate,
    DateOnly ValidUntil,
    string CurrencyCode,
    decimal ExchangeRate,
    IReadOnlyList<CreateDocumentLineRequest> Lines);

public sealed record ConvertSalesQuoteRequest(
    string InvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly DueDate);

public sealed record CreateBillRequest(
    long PartyId,
    string BillNumber,
    DateOnly BillDate,
    DateOnly DueDate,
    string CurrencyCode,
    decimal ExchangeRate,
    IReadOnlyList<CreateDocumentLineRequest> Lines);
public sealed record UpdateBillRequest(
    long PartyId,
    string BillNumber,
    DateOnly BillDate,
    DateOnly DueDate,
    string CurrencyCode,
    decimal ExchangeRate,
    IReadOnlyList<CreateDocumentLineRequest> Lines);

public sealed record CreateDocumentLineRequest(
    long AccountId,
    long? TaxCodeId,
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public sealed record PostDocumentRequest(
    long JournalId,
    long AccountingPeriodId,
    long ControlAccountId);

public sealed record DocumentLineDto(
    long Id,
    long AccountId,
    long? TaxCodeId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal NetAmount,
    decimal TaxRatePercent,
    decimal TaxAmount,
    decimal GrossAmount);

public sealed record InvoiceDto(
    long Id,
    long CompanyId,
    long PartyId,
    long? JournalEntryId,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal NetTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    decimal AllocatedTotal,
    string Status,
    IReadOnlyList<DocumentLineDto> Lines);

public sealed record SalesQuoteDto(
    long Id,
    long CompanyId,
    long CustomerId,
    long? SalesInvoiceId,
    string QuoteNumber,
    DateOnly QuoteDate,
    DateOnly ValidUntil,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal NetTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string Status,
    DateTimeOffset? SentAt,
    string? SentTo,
    IReadOnlyList<DocumentLineDto> Lines);

public sealed record SalesCreditNoteLineDto(
    long Id,
    long SalesInvoiceLineId,
    long AccountId,
    long? TaxCodeId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal NetAmount,
    decimal TaxRatePercent,
    decimal TaxAmount,
    decimal GrossAmount);

public sealed record SalesCreditNoteDto(
    long Id,
    long CompanyId,
    long SalesInvoiceId,
    string InvoiceNumber,
    string CreditNoteNumber,
    DateOnly CreditNoteDate,
    string Reason,
    string CurrencyCode,
    decimal NetTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string Status,
    decimal RefundedAmount,
    decimal RefundableAmount,
    IReadOnlyList<SalesCreditNoteLineDto> Lines);

public sealed record CreateCustomerRefundRequest(
    string RefundNumber,
    DateOnly RefundDate,
    decimal Amount,
    long JournalId,
    long AccountingPeriodId,
    long CashAccountId);

public sealed record CustomerRefundDto(
    long Id,
    long CompanyId,
    long SalesCreditNoteId,
    long JournalEntryId,
    string RefundNumber,
    DateOnly RefundDate,
    decimal Amount,
    decimal FunctionalAmount,
    string Status);

public sealed record BillDto(
    long Id,
    long CompanyId,
    long PartyId,
    long? JournalEntryId,
    string BillNumber,
    DateOnly BillDate,
    DateOnly DueDate,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal NetTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    decimal AllocatedTotal,
    string Status,
    IReadOnlyList<DocumentLineDto> Lines);

public sealed record TaxAccountMapping(long TaxCodeId, long AccountId);

public sealed record PostSalesInvoiceRequest(
    long JournalId,
    long AccountingPeriodId,
    long ReceivableAccountId,
    IReadOnlyList<TaxAccountMapping> TaxAccounts);

public sealed record PostPurchaseBillRequest(
    long JournalId,
    long AccountingPeriodId,
    long PayableAccountId,
    IReadOnlyList<TaxAccountMapping> TaxAccounts);

public sealed record CreatePaymentRequest(
    long PartyId,
    string PaymentNumber,
    string Direction,
    DateOnly PaymentDate,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal Amount);
public sealed record UpdatePaymentRequest(
    long PartyId,
    string PaymentNumber,
    string Direction,
    DateOnly PaymentDate,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal Amount);

public sealed record AllocatePaymentRequest(decimal Amount, long? SalesInvoiceId, long? PurchaseBillId);

public sealed record PostPaymentRequest(
    long JournalId,
    long AccountingPeriodId,
    long CashAccountId,
    long ControlAccountId);

public sealed record PosSaleLineRequest(
    long ItemId,
    string Description,
    long RevenueAccountId,
    long? TaxCodeId,
    decimal Quantity,
    decimal UnitPrice);

public sealed record CheckoutPosSaleRequest(
    long PartyId,
    string InvoiceNumber,
    string PaymentNumber,
    DateOnly SaleDate,
    string CurrencyCode,
    decimal ExchangeRate,
    IReadOnlyList<PosSaleLineRequest> Lines,
    long WarehouseId,
    long JournalId,
    long AccountingPeriodId,
    long ReceivableAccountId,
    IReadOnlyList<TaxAccountMapping> TaxAccounts,
    long SettlementAccountId,
    long InventoryAccountId,
    long CostOfGoodsSoldAccountId);

public sealed record PosSaleDto(InvoiceDto Invoice, PaymentDto Payment);

public sealed record PaymentDto(
    long Id,
    long CompanyId,
    long PartyId,
    long? JournalEntryId,
    string PaymentNumber,
    string Direction,
    DateOnly PaymentDate,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal Amount,
    decimal AllocatedAmount,
    string Status);

public sealed class BillingValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The billing request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class BillingConflictException(string message) : Exception(message);
