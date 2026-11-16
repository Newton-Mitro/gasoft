using Gasoft.Application.Common;

namespace Gasoft.Application.BankReconciliation;

public sealed record BankAccountRequest(
    string? Name,
    string? BankName,
    string? AccountNumberLastFour,
    string? CurrencyCode,
    long ChartOfAccountId);

public sealed record BankAccountDto(
    long Id,
    string Name,
    string BankName,
    string AccountNumberLastFour,
    string CurrencyCode,
    long ChartOfAccountId,
    string ChartOfAccountCode,
    string ChartOfAccountName,
    bool IsActive);

public sealed record BankMatchDto(
    long Id,
    long JournalLineId,
    decimal Amount,
    DateOnly EntryDate,
    string Description);

public sealed record BankTransactionDto(
    long Id,
    DateOnly TransactionDate,
    string Description,
    string? Reference,
    decimal Amount,
    decimal MatchedAmount,
    decimal RemainingAmount,
    IReadOnlyList<BankMatchDto> Matches);

public sealed record BankMatchCandidateDto(
    long JournalLineId,
    DateOnly EntryDate,
    string Description,
    decimal Amount);

public sealed record BankMatchRequest(long JournalLineId, decimal? Amount = null);

public sealed record BankMatchResultDto(long Id, long JournalLineId, decimal Amount);

public sealed record BankAdjustmentRequest(long OffsetAccountId, DateOnly? EntryDate, string? Description);

public sealed record BankAdjustmentResponse(
    long JournalEntryId,
    long JournalLineId,
    decimal Amount,
    string Status);

public sealed record BankStatementImportResultDto(int Imported, int Duplicates, int TotalRows);

public sealed class BankReconciliationValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The bank reconciliation request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class BankReconciliationConflictException(string message) : Exception(message);

public sealed class BankReconciliationNotFoundException(string message) : Exception(message);

public sealed class BankReconciliationConcurrentException(string message, Exception? innerException = null)
    : Exception(message, innerException);
