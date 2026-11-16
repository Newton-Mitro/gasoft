namespace Gasoft.Domain.Entities;

public sealed class CustomerRefund : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long SalesCreditNoteId { get; set; }
    public long JournalEntryId { get; set; }
    public required string RefundNumber { get; set; }
    public DateOnly RefundDate { get; set; }
    public decimal Amount { get; set; }
    public decimal FunctionalAmount { get; set; }
    public string Status { get; set; } = "Posted";
    public JournalEntry JournalEntry { get; set; } = null!;
}
