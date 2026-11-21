namespace Gasoft.Application.Expenses;

public sealed record CreateExpenseReportRequest(
    string Title,
    IReadOnlyList<ExpenseReportLineRequest> Lines);

public sealed record ExpenseReportLineRequest(
    long? Id,
    DateOnly ExpenseDate,
    string Merchant,
    string Description,
    long AccountId,
    decimal Amount);

public sealed record ReviewExpenseReportRequest(string? Note);

public sealed record PostExpenseReportRequest(
    long JournalId,
    long AccountingPeriodId,
    long EmployeePayableAccountId);

public sealed record ReimburseExpenseReportRequest(
    long JournalId,
    long AccountingPeriodId,
    long CashAccountId);

public sealed record ExpenseReportLineDto(
    long Id,
    DateOnly ExpenseDate,
    string Merchant,
    string Description,
    long AccountId,
    decimal Amount,
    string? ReceiptUrl);

public sealed record ExpenseReportDto(
    long Id,
    long CompanyId,
    long EmployeeUserId,
    string EmployeeName,
    string ReportNumber,
    string Title,
    string CurrencyCode,
    decimal TotalAmount,
    string Status,
    string? ReviewNote,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset? PostedAt,
    DateTimeOffset? ReimbursedAt,
    long? JournalEntryId,
    long? ReimbursementJournalEntryId,
    bool CanEdit,
    bool CanSubmit,
    bool CanApprove,
    bool CanPost,
    bool CanReimburse,
    IReadOnlyList<ExpenseReportLineDto> Lines);

public sealed record ExpenseReceiptReference(long ReportId, long LineId, string FileName);

public sealed record ExpenseReportUpdateResult(
    ExpenseReportDto Report,
    IReadOnlyList<ExpenseReceiptReference> RemovedReceipts);

public sealed class ExpenseReportValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The expense report request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class ExpenseReportConflictException(string message) : Exception(message);
