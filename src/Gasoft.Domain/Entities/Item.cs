namespace Gasoft.Domain.Entities;

public sealed class Item : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public required string BaseUnitCode { get; set; }
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public required string CostingMethod { get; set; }
    public bool IsActive { get; set; } = true;
}
