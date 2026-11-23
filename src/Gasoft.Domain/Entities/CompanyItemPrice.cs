namespace Gasoft.Domain.Entities;

public sealed class CompanyItemPrice : TenantOwnedEntity
{
    public long CompanyId { get; set; }
    public long ItemId { get; set; }
    public decimal? UnitSellingPrice { get; set; }
    public required string CurrencyCode { get; set; }
    public Item? Item { get; set; }
}
