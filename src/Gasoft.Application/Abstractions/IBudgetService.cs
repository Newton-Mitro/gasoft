using Gasoft.Application.Budgets;

namespace Gasoft.Application.Abstractions;

public interface IBudgetService
{
    Task<BudgetWorkspaceDto?> GetWorkspaceAsync(
        long companyId,
        long? fiscalYearId,
        CancellationToken cancellationToken);
    Task<BudgetDto?> CreateAsync(long companyId, CreateBudgetRequest request, CancellationToken cancellationToken);
    Task SaveLinesAsync(
        long companyId,
        long budgetId,
        IReadOnlyList<BudgetLineRequest>? lines,
        CancellationToken cancellationToken);
    Task<bool> DeleteAsync(long companyId, long budgetId, CancellationToken cancellationToken);
    Task<BudgetVsActualDto> GetBudgetVsActualAsync(
        long companyId,
        long budgetId,
        CancellationToken cancellationToken);
}
