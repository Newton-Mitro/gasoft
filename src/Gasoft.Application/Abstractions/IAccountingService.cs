using Gasoft.Application.Accounting;

namespace Gasoft.Application.Abstractions;

public interface IAccountingService
{
    Task<IReadOnlyList<AccountGroupDto>> GetAccountGroupsAsync(long companyId, CancellationToken cancellationToken);
    Task<PagedAccountGroupsDto> GetAccountGroupsPageAsync(
        long companyId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken);
    Task<AccountGroupDto?> GetAccountGroupAsync(long companyId, long groupId, CancellationToken cancellationToken);
    Task<AccountGroupDto> CreateAccountGroupAsync(long companyId, CreateAccountGroupRequest request, CancellationToken cancellationToken);
    Task<AccountGroupDto?> UpdateAccountGroupAsync(
        long companyId,
        long groupId,
        UpdateAccountGroupRequest request,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(long companyId, CancellationToken cancellationToken);
    Task<PagedAccountsDto> GetAccountsPageAsync(
        long companyId,
        int page,
        int pageSize,
        long? accountGroupId,
        string? search,
        CancellationToken cancellationToken);
    Task<AccountDto?> GetAccountAsync(long companyId, long accountId, CancellationToken cancellationToken);
    Task<AccountDto?> UpdateAccountAsync(
        long companyId,
        long accountId,
        UpdateAccountRequest request,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<FiscalYearDto>> GetFiscalYearsAsync(long companyId, CancellationToken cancellationToken);
    Task<FiscalYearDto?> GetFiscalYearAsync(long companyId, long fiscalYearId, CancellationToken cancellationToken);
    Task<FiscalYearDto?> UpdateFiscalYearAsync(long companyId, long fiscalYearId, UpdateFiscalYearRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountingPeriodDto>> GetPeriodsAsync(long companyId, CancellationToken cancellationToken);
    Task<AccountingPeriodDto?> GetPeriodAsync(long companyId, long periodId, CancellationToken cancellationToken);
    Task<AccountingPeriodDto?> UpdatePeriodAsync(long companyId, long periodId, UpdateAccountingPeriodRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<JournalDto>> GetJournalsAsync(long companyId, CancellationToken cancellationToken);
    Task<JournalDto?> GetJournalAsync(long companyId, long journalId, CancellationToken cancellationToken);
    Task<JournalDto?> UpdateJournalAsync(long companyId, long journalId, UpdateJournalRequest request, CancellationToken cancellationToken);
    Task<AccountDto> CreateAccountAsync(long companyId, CreateAccountRequest request, CancellationToken cancellationToken);
    Task<FiscalYearDto> CreateFiscalYearAsync(long companyId, CreateFiscalYearRequest request, CancellationToken cancellationToken);
    Task<AccountingPeriodDto> CreatePeriodAsync(long companyId, CreateAccountingPeriodRequest request, CancellationToken cancellationToken);
    Task<JournalDto> CreateJournalAsync(long companyId, CreateJournalRequest request, CancellationToken cancellationToken);
    Task<JournalEntryDto?> CreateDraftAsync(long companyId, CreateJournalEntryRequest request, CancellationToken cancellationToken);
    Task<JournalEntryDto?> UpdateDraftAsync(long companyId, long entryId, CreateJournalEntryRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<JournalEntryDto>> GetJournalEntriesAsync(long companyId, CancellationToken cancellationToken);
    Task<JournalEntryDto?> GetJournalEntryAsync(long companyId, long entryId, CancellationToken cancellationToken);
    Task<JournalEntryDto?> PostAsync(long companyId, long entryId, CancellationToken cancellationToken);
}
