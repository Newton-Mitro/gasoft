namespace Gasoft.Domain.Entities;

public sealed class InventoryCount : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long WarehouseId { get; set; }
    public long SnapshotMovementId { get; set; }
    public long? JournalEntryId { get; set; }
    public required string CountNumber { get; set; }
    public DateOnly CountDate { get; set; }
    public string Status { get; set; } = "Draft";
    public int Revision { get; set; }
}
