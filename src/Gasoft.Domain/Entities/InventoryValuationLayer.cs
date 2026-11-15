namespace Gasoft.Domain.Entities;

public sealed class InventoryValuationLayer : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long StockMovementId { get; set; }
    public long ItemId { get; set; }
    public long WarehouseId { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public int Revision { get; set; }
}
