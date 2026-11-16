namespace Gasoft.Domain.Entities;

public sealed class PurchaseOrderLine : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long PurchaseOrderId { get; set; }
    public long ItemId { get; set; }
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public int Revision { get; set; }
}
