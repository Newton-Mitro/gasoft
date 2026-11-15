namespace Gasoft.Domain.Entities;

public sealed class Company : TenantOwnedEntity
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string LegalName { get; set; }
    public required string FunctionalCurrencyCode { get; set; }
}
