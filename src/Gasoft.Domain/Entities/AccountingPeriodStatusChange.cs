namespace Gasoft.Domain.Entities;

public sealed class AccountingPeriodStatusChange : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long AccountingPeriodId { get; set; }
    public long ChangedByUserId { get; set; }
    public required string FromStatus { get; set; }
    public required string ToStatus { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
