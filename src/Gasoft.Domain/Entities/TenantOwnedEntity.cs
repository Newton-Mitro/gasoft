namespace Gasoft.Domain.Entities;

public abstract class TenantOwnedEntity
{
    public long TenantId { get; set; }
}
