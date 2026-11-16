using Gasoft.Application.Reports;

namespace Gasoft.Application.Abstractions;

public interface IReportingService
{
    Task<CashFlowReportDto?> GetCashFlowAsync(
        long companyId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<CashFlowAccountDto>> GetCashFlowAccountsAsync(
        long companyId,
        CancellationToken cancellationToken);
    Task<CashFlowCategoryDto?> UpdateCashFlowCategoryAsync(
        long companyId,
        long accountId,
        CashFlowCategoryRequest request,
        CancellationToken cancellationToken);
    Task<TrialBalanceReportDto?> GetTrialBalanceAsync(
        long companyId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken);
    Task<IncomeStatementReportDto?> GetIncomeStatementAsync(
        long companyId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken);
    Task<BalanceSheetReportDto?> GetBalanceSheetAsync(
        long companyId,
        DateOnly asOfDate,
        CancellationToken cancellationToken);
    Task<AgingReportDto?> GetReceivablesAgingAsync(
        long companyId,
        DateOnly asOfDate,
        CancellationToken cancellationToken);
    Task<AgingReportDto?> GetPayablesAgingAsync(
        long companyId,
        DateOnly asOfDate,
        CancellationToken cancellationToken);
}
