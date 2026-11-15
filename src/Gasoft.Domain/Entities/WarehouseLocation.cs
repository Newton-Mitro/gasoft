namespace Gasoft.Domain.Entities;

public sealed class WarehouseLocation : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long WarehouseId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
}
