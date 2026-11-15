namespace Gasoft.Domain.Entities;

public sealed class TaxJurisdiction
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string CountryCode { get; set; }
}
