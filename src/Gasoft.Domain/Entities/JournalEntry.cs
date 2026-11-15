namespace Gasoft.Domain.Entities;

public sealed class JournalEntry : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long JournalId { get; set; }
    public long AccountingPeriodId { get; set; }
    public DateOnly EntryDate { get; set; }
    public required string Description { get; set; }
    public string Status { get; set; } = "Draft";
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public int Revision { get; set; }
    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}
