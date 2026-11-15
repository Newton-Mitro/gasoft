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
