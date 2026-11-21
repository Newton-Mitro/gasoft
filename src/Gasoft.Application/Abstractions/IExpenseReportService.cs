using Gasoft.Application.Expenses;

namespace Gasoft.Application.Abstractions;

public interface IExpenseReportService
{
    Task<IReadOnlyList<ExpenseReportDto>> GetReportsAsync(
        long companyId,
        bool includeAll,
        CancellationToken cancellationToken);
    Task<ExpenseReportDto?> GetReportAsync(
        long companyId,
        long reportId,
        bool includeAll,
        CancellationToken cancellationToken);
    Task<ExpenseReportDto> CreateReportAsync(
        long companyId,
        CreateExpenseReportRequest request,
        CancellationToken cancellationToken);
    Task<ExpenseReportUpdateResult?> UpdateReportAsync(
        long companyId,
        long reportId,
        CreateExpenseReportRequest request,
        CancellationToken cancellationToken);
    Task<ExpenseReportDto?> SubmitReportAsync(
        long companyId,
        long reportId,
        CancellationToken cancellationToken);
    Task<ExpenseReportDto?> ReviewReportAsync(
        long companyId,
        long reportId,
        bool approve,
        ReviewExpenseReportRequest request,
        CancellationToken cancellationToken);
    Task<ExpenseReportDto?> PostReportAsync(
        long companyId,
        long reportId,
        PostExpenseReportRequest request,
        CancellationToken cancellationToken);
    Task<ExpenseReportDto?> ReimburseReportAsync(
        long companyId,
        long reportId,
        ReimburseExpenseReportRequest request,
        CancellationToken cancellationToken);
    Task<string?> GetReceiptFileNameAsync(
        long companyId,
        long reportId,
        long lineId,
        bool includeAll,
        CancellationToken cancellationToken);
    Task<ExpenseReportDto?> SetReceiptAsync(
        long companyId,
        long reportId,
        long lineId,
        string fileName,
        bool includeAll,
        CancellationToken cancellationToken);
}
