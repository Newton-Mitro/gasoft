namespace Gasoft.Domain.Entities;

public sealed class StockMovement : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long ItemId { get; set; }
    public long WarehouseId { get; set; }
    public long? WarehouseLocationId { get; set; }
    public long? JournalEntryId { get; set; }
    public required string MovementNumber { get; set; }
    public required string MovementType { get; set; }
    public DateOnly MovementDate { get; set; }
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public int Revision { get; set; }
}
