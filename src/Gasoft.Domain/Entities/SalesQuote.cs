namespace Gasoft.Domain.Entities;

public sealed class SalesQuote : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long CustomerId { get; set; }
    public long? SalesInvoiceId { get; set; }
    public required string QuoteNumber { get; set; }
    public DateOnly QuoteDate { get; set; }
    public DateOnly ValidUntil { get; set; }
    public required string CurrencyCode { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTimeOffset? SentAt { get; set; }
    public string? SentTo { get; set; }
    public int Revision { get; set; }
    public ICollection<SalesQuoteLine> Lines { get; set; } = new List<SalesQuoteLine>();
}
