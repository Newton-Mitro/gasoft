namespace Gasoft.Domain.Entities;

public sealed class TenantTaxRegistration : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long JurisdictionId { get; set; }
    public required string RegistrationNumber { get; set; }
}
