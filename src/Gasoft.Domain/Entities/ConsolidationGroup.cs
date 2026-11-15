namespace Gasoft.Domain.Entities;

public sealed class ConsolidationGroup : TenantOwnedEntity
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string ReportingCurrencyCode { get; set; }
}
