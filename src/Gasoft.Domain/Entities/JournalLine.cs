namespace Gasoft.Domain.Entities;

public sealed class JournalLine : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long JournalEntryId { get; set; }
    public long AccountId { get; set; }
    public long? CostCenterId { get; set; }
    public required string TransactionCurrencyCode { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal FunctionalDebit { get; set; }
    public decimal FunctionalCredit { get; set; }
    public decimal ExchangeRate { get; set; }
    public string? Description { get; set; }
}
