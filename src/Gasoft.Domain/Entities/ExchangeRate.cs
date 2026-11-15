namespace Gasoft.Domain.Entities;

public sealed class ExchangeRate
{
    public long Id { get; set; }
    public required string SourceCurrencyCode { get; set; }
    public required string TargetCurrencyCode { get; set; }
    public DateOnly EffectiveOn { get; set; }
    public decimal Rate { get; set; }
}
