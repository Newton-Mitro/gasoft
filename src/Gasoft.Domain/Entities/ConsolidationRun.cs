namespace Gasoft.Domain.Entities;

public sealed class ConsolidationRun : TenantOwnedEntity
{
    public long Id { get; set; }
    public long ConsolidationGroupId { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string Status { get; set; } = "Draft";
}
