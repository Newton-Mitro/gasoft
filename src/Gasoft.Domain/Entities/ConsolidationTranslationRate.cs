namespace Gasoft.Domain.Entities;

public sealed class ConsolidationTranslationRate : TenantOwnedEntity
{
    public long Id { get; set; }
    public long ConsolidationRunId { get; set; }
    public long CompanyId { get; set; }
    public required string SourceCurrencyCode { get; set; }
    public required string TargetCurrencyCode { get; set; }
    public decimal ClosingRate { get; set; }
    public decimal AverageRate { get; set; }
}
