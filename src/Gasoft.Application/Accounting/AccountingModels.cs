namespace Gasoft.Application.Accounting;

public sealed record CreateAccountGroupRequest(string Code, string Name, string AccountType);
public sealed record UpdateAccountGroupRequest(string Code, string Name, string AccountType);
public sealed record CreateAccountRequest(
    long AccountGroupId,
    string Code,
    string Name,
    long? ParentId,
    bool AllowPosting);
public sealed record UpdateAccountRequest(
    long AccountGroupId,
    string Code,
    string Name,
    long? ParentId,
    bool AllowPosting,
    bool IsActive,
    bool DisableParentPosting = false);
public sealed record CreateFiscalYearRequest(DateOnly StartsOn, DateOnly EndsOn);
public sealed record UpdateFiscalYearRequest(DateOnly StartsOn, DateOnly EndsOn);
public sealed record CreateAccountingPeriodRequest(
    long FiscalYearId,
    DateOnly StartsOn,
    DateOnly EndsOn);
public sealed record UpdateAccountingPeriodRequest(
    long FiscalYearId,
    DateOnly StartsOn,
    DateOnly EndsOn);
public sealed record ChangeAccountingPeriodStatusRequest(string? Reason);
public sealed record CreateJournalRequest(string Code, string Name, string JournalType);
public sealed record UpdateJournalRequest(string Code, string Name, string JournalType, bool IsActive);
public sealed record CreateJournalEntryRequest(
    long JournalId,
    long AccountingPeriodId,
    DateOnly EntryDate,
    string Description,
    IReadOnlyList<CreateJournalLineRequest> Lines);
public sealed record ReverseJournalEntryRequest(DateOnly ReversalDate);
public sealed record CreateJournalLineRequest(
    long AccountId,
    string TransactionCurrencyCode,
    decimal Debit,
    decimal Credit,
    decimal ExchangeRate,
    string? Description);

public sealed record AccountGroupDto(
    long Id,
    long CompanyId,
    string Code,
    string Name,
    string AccountType,
    string NormalBalance);
public sealed record PagedAccountGroupsDto(
    IReadOnlyList<AccountGroupDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
public sealed record AccountDto(
    long Id,
    long CompanyId,
    long AccountGroupId,
    long? ParentId,
    string Code,
    string Name,
    bool AllowPosting,
    bool IsActive);
public sealed record PagedAccountsDto(
    IReadOnlyList<AccountDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    int TotalAccounts);
public sealed record FiscalYearDto(long Id, long CompanyId, DateOnly StartsOn, DateOnly EndsOn, string Status);
public sealed record AccountingPeriodDto(
    long Id,
    long FiscalYearId,
    DateOnly StartsOn,
    DateOnly EndsOn,
    string Status);
public sealed record AccountingPeriodStatusChangeDto(
    long Id,
    string FromStatus,
    string ToStatus,
    string Reason,
    DateTimeOffset ChangedAt,
    long ChangedByUserId,
    string ChangedByDisplayName);
public sealed record AccountingPeriodCloseChecklistDto(
    long PeriodId,
    int DraftJournalEntryCount,
    int UnreconciledBankTransactionCount)
{
    public bool CanClose => DraftJournalEntryCount == 0 && UnreconciledBankTransactionCount == 0;
}
public sealed record FiscalYearCloseChecklistDto(
    long FiscalYearId,
    int AccountingPeriodCount,
    int OpenAccountingPeriodCount,
    int DraftJournalEntryCount,
    bool ClosingJournalRequired,
    long? ClosingJournalEntryId,
    string? ClosingJournalStatus,
    IReadOnlyList<FiscalYearClosingLineDto> ClosingJournalLines)
{
    public bool CanClose =>
        AccountingPeriodCount > 0 &&
        OpenAccountingPeriodCount == 0 &&
        DraftJournalEntryCount == 0 &&
        (!ClosingJournalRequired || ClosingJournalStatus == "Posted");
}
public sealed record FiscalYearClosingLineDto(
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit);
public sealed record PrepareFiscalYearClosingRequest(long RetainedEarningsAccountId);
public sealed record FiscalYearStatusChangeDto(
    long Id,
    string FromStatus,
    string ToStatus,
    string Reason,
    DateTimeOffset ChangedAt,
    long ChangedByUserId,
    string ChangedByDisplayName);
public sealed record JournalDto(long Id, long CompanyId, string Code, string Name, string JournalType, bool IsActive);
public sealed record JournalEntryDto(
    long Id,
    long CompanyId,
    long JournalId,
    long AccountingPeriodId,
    DateOnly EntryDate,
    string Description,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyList<JournalLineDto> Lines,
    long? ReversalOfEntryId = null,
    long? ReversedByEntryId = null);
public sealed record JournalLineDto(
    long Id,
    long AccountId,
    string TransactionCurrencyCode,
    decimal Debit,
    decimal Credit,
    decimal FunctionalDebit,
    decimal FunctionalCredit,
    decimal ExchangeRate,
    string? Description);

public sealed class AccountingValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The accounting request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class AccountingConflictException(string message) : Exception(message);
