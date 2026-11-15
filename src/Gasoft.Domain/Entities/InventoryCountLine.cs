namespace Gasoft.Domain.Entities;

public sealed class InventoryCountLine : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long InventoryCountId { get; set; }
    public long ItemId { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal SystemValue { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal? AdjustmentUnitCost { get; set; }
    public int Revision { get; set; }
}
