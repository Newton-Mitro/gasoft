namespace Gasoft.Domain.Entities;

public sealed class PurchaseBill : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long SupplierId { get; set; }
    public long? JournalEntryId { get; set; }
    public required string BillNumber { get; set; }
    public DateOnly BillDate { get; set; }
    public DateOnly DueDate { get; set; }
    public required string CurrencyCode { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal AllocatedTotal { get; set; }
    public string Status { get; set; } = "Draft";
    public int Revision { get; set; }
    public ICollection<PurchaseBillLine> Lines { get; set; } = new List<PurchaseBillLine>();
}
