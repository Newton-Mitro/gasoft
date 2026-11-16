using Gasoft.Application.Common;

namespace Gasoft.Application.FixedAssets;

public sealed record CreateFixedAssetRequest(
    string? AssetNumber,
    string? Name,
    string? Description,
    DateOnly AcquisitionDate,
    DateOnly InServiceDate,
    decimal AcquisitionCost,
    decimal ResidualValue,
    int UsefulLifeMonths,
    long AccountingPeriodId,
    long JournalId,
    long AssetAccountId,
    long AccumulatedDepreciationAccountId,
    long DepreciationExpenseAccountId,
    long CreditAccountId);

public sealed record DepreciateFixedAssetRequest(long ThroughAccountingPeriodId, long JournalId);

public sealed record DisposeFixedAssetRequest(
    DateOnly DisposalDate,
    decimal Proceeds,
    long AccountingPeriodId,
    long JournalId,
    long CashAccountId,
    long GainAccountId,
    long LossAccountId);

public sealed record CreatedFixedAssetDto(long Id, string AssetNumber);

public sealed record FixedAssetDepreciationResultDto(
    long AssetId,
    int PostedPeriods,
    decimal PostedAmount,
    decimal AccumulatedDepreciation,
    decimal NetBookValue);

public sealed record FixedAssetDisposalResultDto(
    long Id,
    string Status,
    DateOnly? DisposalDate,
    decimal? DisposalProceeds,
    decimal Gain,
    decimal Loss,
    decimal CarryingAmount);

public sealed record FixedAssetDto(
    long Id,
    string AssetNumber,
    string Name,
    string? Description,
    DateOnly AcquisitionDate,
    DateOnly InServiceDate,
    decimal AcquisitionCost,
    decimal ResidualValue,
    int UsefulLifeMonths,
    long AssetAccountId,
    long AccumulatedDepreciationAccountId,
    long DepreciationExpenseAccountId,
    long AcquisitionJournalEntryId,
    string Status,
    DateOnly? DisposalDate,
    decimal? DisposalProceeds,
    long? DisposalJournalEntryId,
    decimal AccumulatedDepreciation,
    decimal NetBookValue,
    string AssetAccountName,
    string AccumulatedDepreciationAccountName,
    string DepreciationExpenseAccountName);

public sealed record FixedAssetPeriodDto(long Id, DateOnly StartsOn, DateOnly EndsOn, string Status);

public sealed record FixedAssetJournalDto(long Id, string Code, string Name);

public sealed record FixedAssetAccountDto(long AccountId, string Code, string Name, string AccountType);

public sealed record FixedAssetDepreciationHistoryDto(
    long AssetId,
    DateOnly DepreciationDate,
    decimal Amount,
    long JournalEntryId);

public sealed record FixedAssetWorkspaceDto(
    string CurrencyCode,
    IReadOnlyList<FixedAssetDto> Assets,
    IReadOnlyList<FixedAssetPeriodDto> Periods,
    IReadOnlyList<FixedAssetJournalDto> Journals,
    IReadOnlyList<FixedAssetAccountDto> Accounts,
    IReadOnlyList<FixedAssetDepreciationHistoryDto> DepreciationHistory);

public sealed record PagedFixedAssetWorkspaceDto(
    string CurrencyCode,
    PagedResultDto<FixedAssetDto> Assets,
    IReadOnlyList<FixedAssetPeriodDto> Periods,
    IReadOnlyList<FixedAssetJournalDto> Journals,
    IReadOnlyList<FixedAssetAccountDto> Accounts,
    IReadOnlyList<FixedAssetDepreciationHistoryDto> DepreciationHistory);

public sealed class FixedAssetValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The fixed-asset request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class FixedAssetConflictException(string message) : Exception(message);

public sealed class FixedAssetNotFoundException(string message) : Exception(message);
