namespace Gasoft.Domain.Entities;

public sealed class TaxRate
{
    public long Id { get; set; }
    public long TaxCodeId { get; set; }
    public decimal RatePercent { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}
