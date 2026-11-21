namespace Gasoft.Domain.Entities;

public sealed class ExpenseReport : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long EmployeeUserId { get; set; }
    public long? ReviewerUserId { get; set; }
    public long? JournalEntryId { get; set; }
    public long? ReimbursementJournalEntryId { get; set; }
    public long? EmployeePayableAccountId { get; set; }
    public long? ReimbursementCashAccountId { get; set; }
    public required string ReportNumber { get; set; }
    public required string Title { get; set; }
    public required string CurrencyCode { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Draft";
    public string? ReviewNote { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset? PostedAt { get; set; }
    public DateTimeOffset? ReimbursedAt { get; set; }
    public int Revision { get; set; }
    public ICollection<ExpenseReportLine> Lines { get; set; } = new List<ExpenseReportLine>();
}
