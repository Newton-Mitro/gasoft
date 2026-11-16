using Gasoft.Application.Billing;
using Gasoft.Application.Common;

namespace Gasoft.Application.Abstractions;

public interface ISalesQuoteService
{
    Task<IReadOnlyList<SalesQuoteDto>?> GetQuotesAsync(long companyId, CancellationToken cancellationToken);
    Task<PagedResultDto<SalesQuoteDto>?> GetQuotesPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken);
    Task<SalesQuoteDto?> GetQuoteAsync(long companyId, long quoteId, CancellationToken cancellationToken);
    Task<SalesQuoteDto> CreateQuoteAsync(long companyId, CreateSalesQuoteRequest request, CancellationToken cancellationToken);
    Task<SalesQuoteDto?> UpdateQuoteAsync(long companyId, long quoteId, UpdateSalesQuoteRequest request, CancellationToken cancellationToken);
    Task<SalesQuoteStatusDto?> SendQuoteAsync(long companyId, long quoteId, CancellationToken cancellationToken);
    Task<SalesQuoteStatusDto?> TransitionQuoteAsync(long companyId, long quoteId, string action, CancellationToken cancellationToken);
    Task<ConvertedSalesQuoteDto?> ConvertQuoteAsync(long companyId, long quoteId, ConvertSalesQuoteRequest request, CancellationToken cancellationToken);
}

public sealed record SalesQuoteStatusDto(long Id, string Status, DateTimeOffset? SentAt, string? SentTo, int Revision);

public sealed record ConvertedSalesQuoteDto(long QuoteId, long InvoiceId, string Status);

public enum SalesQuoteDeliveryFailure
{
    Unavailable,
    EmailRejected,
    StatusUpdateFailed
}

public sealed class SalesQuoteDeliveryException(
    SalesQuoteDeliveryFailure failure,
    string message,
    Exception? innerException = null) : Exception(message, innerException)
{
    public SalesQuoteDeliveryFailure Failure { get; } = failure;
}
