using Gasoft.Application.Accounting;
using Gasoft.Application.Common;

namespace Gasoft.Application.Abstractions;

public interface IJournalEntryService
{
    Task<JournalEntryDto?> CreateDraftAsync(long companyId, CreateJournalEntryRequest request, CancellationToken cancellationToken);
    Task<JournalEntryDto?> UpdateDraftAsync(long companyId, long entryId, CreateJournalEntryRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<JournalEntryDto>> GetJournalEntriesAsync(long companyId, CancellationToken cancellationToken);
    Task<PagedResultDto<JournalEntryDto>> GetJournalEntriesPageAsync(
        long companyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<JournalEntryDto?> GetJournalEntryAsync(long companyId, long entryId, CancellationToken cancellationToken);
    Task<JournalEntryDto?> ReverseJournalEntryAsync(
        long companyId,
        long entryId,
        ReverseJournalEntryRequest request,
        CancellationToken cancellationToken);
    Task<JournalEntryDto?> PostAsync(long companyId, long entryId, CancellationToken cancellationToken);
}
