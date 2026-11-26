namespace Gasoft.Domain.Entities;

public sealed class SalesInvoice : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long CustomerId { get; set; }
    public long? JournalEntryId { get; set; }
    public required string InvoiceNumber { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public required string CurrencyCode { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal AllocatedTotal { get; set; }
    public decimal CreditedTotal { get; set; }
    public decimal RefundedTotal { get; set; }
    public string Status { get; set; } = "Draft";
    public int Revision { get; set; }
    public ICollection<SalesInvoiceLine> Lines { get; set; } = new List<SalesInvoiceLine>();
}
