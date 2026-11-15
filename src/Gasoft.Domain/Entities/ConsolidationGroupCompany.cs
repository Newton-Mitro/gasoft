namespace Gasoft.Domain.Entities;

public sealed class ConsolidationGroupCompany : TenantOwnedEntity
{
    public long ConsolidationGroupId { get; set; }
    public long CompanyId { get; set; }
}
