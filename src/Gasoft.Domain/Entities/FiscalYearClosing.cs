namespace Gasoft.Domain.Entities;

public sealed class FiscalYearClosing : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long FiscalYearId { get; set; }
    public long JournalEntryId { get; set; }
    public long RetainedEarningsAccountId { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
