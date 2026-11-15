namespace Gasoft.Domain.Entities;

public sealed class CostCenter : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}
