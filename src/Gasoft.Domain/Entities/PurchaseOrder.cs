namespace Gasoft.Domain.Entities;

public sealed class PurchaseOrder : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long SupplierId { get; set; }
    public long WarehouseId { get; set; }
    public required string OrderNumber { get; set; }
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public required string CurrencyCode { get; set; }
    public decimal ExchangeRate { get; set; }
    public string Status { get; set; } = "Draft";
    public int Revision { get; set; }
    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}
