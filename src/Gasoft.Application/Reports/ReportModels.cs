namespace Gasoft.Application.Reports;

public sealed record CashFlowAccountDto(
    long AccountId,
    string Code,
    string Name,
    string AccountType,
    string? CashFlowCategory,
    bool IsCashAccount);

public sealed record CashFlowCategoryRequest(string? Category);

public sealed record CashFlowCategoryDto(long AccountId, string? Category);

public sealed record CashFlowAccountBalanceDto(
    long AccountId,
    string Code,
    string AccountName,
    string BankName,
    decimal OpeningBalance,
    decimal CashChange,
    decimal ClosingBalance);

public sealed record CashFlowAccountFlowDto(
    long AccountId,
    string AccountCode,
    string AccountName,
    decimal Inflows,
    decimal Outflows,
    decimal Net);

public sealed record CashFlowSectionDto(
    string Category,
    decimal Inflows,
    decimal Outflows,
    decimal Net,
    IReadOnlyList<CashFlowAccountFlowDto> Accounts);

public sealed record DirectCashFlowDto(
    CashFlowSectionDto Operating,
    CashFlowSectionDto Investing,
    CashFlowSectionDto Financing,
    CashFlowSectionDto Unclassified,
    decimal NetCashIncrease);

public sealed record IndirectCashFlowDto(
    decimal NetIncome,
    decimal NonCashAdjustments,
    decimal WorkingCapitalAdjustments,
    decimal OperatingCashFlow,
    decimal InvestingCashFlow,
    decimal FinancingCashFlow,
    decimal UnclassifiedCashFlow,
    decimal UnclassifiedInflows,
    decimal UnclassifiedOutflows,
    decimal NetCashIncrease,
    decimal ActualCashMovement,
    decimal ReconciliationDifference);

public sealed record CashFlowReportDto(
    long CompanyId,
    string CurrencyCode,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<CashFlowAccountBalanceDto> CashAccounts,
    DirectCashFlowDto Direct,
    IndirectCashFlowDto Indirect,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal ActualCashMovement);

public sealed record TrialBalanceAccountDto(
    long AccountId,
    string AccountCode,
    string AccountName,
    string AccountType,
    decimal Debit,
    decimal Credit);

public sealed record TrialBalanceReportDto(
    long CompanyId,
    string CurrencyCode,
    DateOnly? From,
    DateOnly? To,
    IReadOnlyList<TrialBalanceAccountDto> Accounts,
    decimal TotalDebit,
    decimal TotalCredit);

public sealed record IncomeStatementAccountDto(
    long AccountId,
    string AccountCode,
    string AccountName,
    string AccountType,
    decimal Amount);

public sealed record IncomeStatementReportDto(
    long CompanyId,
    string CurrencyCode,
    DateOnly? From,
    DateOnly? To,
    IReadOnlyList<IncomeStatementAccountDto> Accounts,
    decimal TotalRevenue,
    decimal TotalExpenses,
    decimal NetIncome);

public sealed record BalanceSheetAccountDto(
    long? AccountId,
    string AccountCode,
    string AccountName,
    string AccountType,
    decimal Balance);

public sealed record BalanceSheetReportDto(
    long CompanyId,
    string CurrencyCode,
    DateOnly AsOfDate,
    IReadOnlyList<BalanceSheetAccountDto> Accounts,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal CurrentEarnings,
    decimal TotalLiabilitiesAndEquity,
    decimal BalanceDifference);

public sealed record AgingDocumentDto(
    long DocumentId,
    string DocumentNumber,
    string PartyNumber,
    string PartyName,
    DateOnly DocumentDate,
    DateOnly DueDate,
    string CurrencyCode,
    decimal OriginalAmount,
    decimal OutstandingAmount,
    int DaysPastDue,
    string AgingBucket);

public sealed record AgingCurrencyTotalDto(
    string CurrencyCode,
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Over90Days,
    decimal Total);

public sealed record AgingReportDto(
    long CompanyId,
    string CompanyFunctionalCurrencyCode,
    DateOnly AsOfDate,
    IReadOnlyList<AgingDocumentDto> Documents,
    IReadOnlyList<AgingCurrencyTotalDto> TotalsByCurrency);

public sealed class ReportValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The report request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
