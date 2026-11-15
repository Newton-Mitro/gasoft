namespace Gasoft.Domain.Entities;

public sealed class Warehouse : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
}
