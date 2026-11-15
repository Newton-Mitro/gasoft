namespace Gasoft.Domain.Entities;

public sealed class Currency
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public byte DecimalPlaces { get; set; }
}
