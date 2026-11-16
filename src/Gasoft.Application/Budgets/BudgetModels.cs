namespace Gasoft.Application.Budgets;

public sealed record CreateBudgetRequest(string? Name, long FiscalYearId);

public sealed record BudgetLineRequest(long AccountId, long AccountingPeriodId, decimal Amount);

public sealed record BudgetDto(long Id, string Name, long FiscalYearId);

public sealed record BudgetFiscalYearDto(long Id, DateOnly StartsOn, DateOnly EndsOn, string Status);

public sealed record BudgetPeriodDto(long Id, DateOnly StartsOn, DateOnly EndsOn, string Status);

public sealed record BudgetAccountDto(long AccountId, string Code, string Name, string AccountType);

public sealed record BudgetWorkspaceDto(
    IReadOnlyList<BudgetFiscalYearDto> FiscalYears,
    IReadOnlyList<BudgetDto> Budgets,
    IReadOnlyList<BudgetPeriodDto> Periods,
    IReadOnlyList<BudgetAccountDto> Accounts);

public sealed record BudgetPeriodValueDto(
    long PeriodId,
    decimal Budget,
    decimal Actual,
    decimal Variance,
    decimal FavorableVariance);

public sealed record BudgetReportAccountDto(
    long AccountId,
    string AccountCode,
    string AccountName,
    string AccountType,
    IReadOnlyList<BudgetPeriodValueDto> Periods,
    decimal Budget,
    decimal Actual,
    decimal FavorableVariance);

public sealed record BudgetSummaryDto(
    decimal RevenueBudget,
    decimal RevenueActual,
    decimal ExpenseBudget,
    decimal ExpenseActual,
    decimal NetIncomeBudget,
    decimal NetIncomeActual);

public sealed record BudgetVsActualDto(
    BudgetDto Budget,
    string CurrencyCode,
    IReadOnlyList<BudgetPeriodDto> Periods,
    IReadOnlyList<BudgetReportAccountDto> Accounts,
    BudgetSummaryDto Summary);

public sealed class BudgetValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The budget request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class BudgetConflictException(string message) : Exception(message);
