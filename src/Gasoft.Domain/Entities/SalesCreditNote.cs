namespace Gasoft.Domain.Entities;

public sealed class SalesCreditNote : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long SalesInvoiceId { get; set; }
    public long? JournalEntryId { get; set; }
    public long? ReceivableAccountId { get; set; }
    public required string CreditNoteNumber { get; set; }
    public DateOnly CreditNoteDate { get; set; }
    public required string Reason { get; set; }
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Status { get; set; } = "Draft";
    public int Revision { get; set; }
    public SalesInvoice SalesInvoice { get; set; } = null!;
    public JournalEntry? JournalEntry { get; set; }
    public ICollection<SalesCreditNoteLine> Lines { get; set; } = new List<SalesCreditNoteLine>();
    public ICollection<CustomerRefund> Refunds { get; set; } = new List<CustomerRefund>();
}
