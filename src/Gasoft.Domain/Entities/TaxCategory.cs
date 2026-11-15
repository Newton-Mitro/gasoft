namespace Gasoft.Domain.Entities;

public sealed class TaxCategory
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
}
