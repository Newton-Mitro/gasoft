namespace Gasoft.Domain.Entities;

public sealed class SalesQuoteLine : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long SalesQuoteId { get; set; }
    public long AccountId { get; set; }
    public long? TaxCodeId { get; set; }
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxRatePercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrossAmount { get; set; }
}
