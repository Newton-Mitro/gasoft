using Gasoft.Application.Accounting;
using Gasoft.Application.Common;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class AccountingService
{
    public Task<JournalEntryDto?> CreateDraftAsync(
        long companyId,
        CreateJournalEntryRequest request,
        CancellationToken cancellationToken) =>
        journalEntries.CreateDraftAsync(companyId, request, cancellationToken);

    public Task<JournalEntryDto?> UpdateDraftAsync(
        long companyId,
        long entryId,
        CreateJournalEntryRequest request,
        CancellationToken cancellationToken) =>
        journalEntries.UpdateDraftAsync(companyId, entryId, request, cancellationToken);

    public Task<IReadOnlyList<JournalEntryDto>> GetJournalEntriesAsync(
        long companyId,
        CancellationToken cancellationToken) =>
        journalEntries.GetJournalEntriesAsync(companyId, cancellationToken);

    public Task<PagedResultDto<JournalEntryDto>> GetJournalEntriesPageAsync(
        long companyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        journalEntries.GetJournalEntriesPageAsync(companyId, page, pageSize, cancellationToken);

    public Task<JournalEntryDto?> GetJournalEntryAsync(
        long companyId,
        long entryId,
        CancellationToken cancellationToken) =>
        journalEntries.GetJournalEntryAsync(companyId, entryId, cancellationToken);

    public Task<JournalEntryDto?> ReverseJournalEntryAsync(
        long companyId,
        long entryId,
        ReverseJournalEntryRequest request,
        CancellationToken cancellationToken) =>
        journalEntries.ReverseJournalEntryAsync(companyId, entryId, request, cancellationToken);

    public Task<JournalEntryDto?> PostAsync(
        long companyId,
        long entryId,
        CancellationToken cancellationToken) =>
        journalEntries.PostAsync(companyId, entryId, cancellationToken);
}
