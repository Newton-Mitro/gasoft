namespace Gasoft.Domain.Entities;

public sealed class Payment : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long PartyId { get; set; }
    public long? JournalEntryId { get; set; }
    public required string PaymentNumber { get; set; }
    public required string Direction { get; set; }
    public DateOnly PaymentDate { get; set; }
    public required string CurrencyCode { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal Amount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public string Status { get; set; } = "Draft";
    public int Revision { get; set; }
}
