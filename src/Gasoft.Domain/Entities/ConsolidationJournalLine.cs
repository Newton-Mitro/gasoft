namespace Gasoft.Domain.Entities;

public sealed class ConsolidationJournalLine : TenantOwnedEntity
{
    public long Id { get; set; }
    public long ConsolidationRunId { get; set; }
    public long CompanyId { get; set; }
    public required string AccountCode { get; set; }
    public required string AccountName { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal ExchangeRate { get; set; }
}
