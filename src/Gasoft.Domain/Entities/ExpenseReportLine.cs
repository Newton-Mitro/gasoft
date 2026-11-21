namespace Gasoft.Domain.Entities;

public sealed class ExpenseReportLine : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long ExpenseReportId { get; set; }
    public long AccountId { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public required string Merchant { get; set; }
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public string? ReceiptFileName { get; set; }
    public ExpenseReport ExpenseReport { get; set; } = null!;
}
