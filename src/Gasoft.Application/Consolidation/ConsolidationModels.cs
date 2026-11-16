namespace Gasoft.Application.Consolidation;

public sealed record ConsolidationGroupRequest(
    string? Code,
    string? Name,
    string? ReportingCurrencyCode,
    long[]? CompanyIds);

public sealed record CreateConsolidationRunRequest(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<ConsolidationTranslationRateRequest>? TranslationRates);

public sealed record ConsolidationTranslationRateRequest(
    long CompanyId,
    decimal ClosingRate,
    decimal AverageRate);

public sealed record SaveEliminationsRequest(
    IReadOnlyList<ConsolidationEliminationRequest>? Lines);

public sealed record ConsolidationEliminationRequest(
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit,
    string? Description);

public sealed record ConsolidationCompanyResponse(
    long Id,
    string Code,
    string LegalName,
    string FunctionalCurrencyCode);

public sealed record ConsolidationGroupResponse(
    long Id,
    string Code,
    string Name,
    string ReportingCurrencyCode,
    IReadOnlyList<ConsolidationCompanyResponse> Companies);

public sealed record ConsolidationRunSummaryDto(long Id, DateOnly PeriodEnd, string Status);

public sealed record ConsolidatedAccountResponse(
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit);

public sealed record ConsolidationTranslationRateResponse(
    long CompanyId,
    string SourceCurrencyCode,
    string TargetCurrencyCode,
    decimal ClosingRate,
    decimal AverageRate);

public sealed record ConsolidationAdjustmentResponse(
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit,
    string? Description);

public sealed record ConsolidationRunResponse(
    long Id,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Status,
    string ReportingCurrencyCode,
    IReadOnlyList<ConsolidatedAccountResponse> Accounts,
    IReadOnlyList<ConsolidationTranslationRateResponse> TranslationRates,
    IReadOnlyList<ConsolidationAdjustmentResponse> Eliminations,
    IReadOnlyList<ConsolidationAdjustmentResponse> TranslationAdjustments);

public sealed record FinalizedConsolidationRunDto(long Id, string Status);

public sealed class ConsolidationValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The consolidation request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class ConsolidationConflictException(string message) : Exception(message);

public sealed class ConsolidationNotFoundException(string message) : Exception(message);
