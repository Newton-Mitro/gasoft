namespace Gasoft.Domain.Entities;

public sealed class ItemCategory : TenantOwnedEntity
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
}
