namespace Gasoft.Domain.Entities;

public sealed class FiscalYearStatusChange : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long FiscalYearId { get; set; }
    public long ChangedByUserId { get; set; }
    public required string FromStatus { get; set; }
    public required string ToStatus { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
