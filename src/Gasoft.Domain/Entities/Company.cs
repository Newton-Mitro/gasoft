namespace Gasoft.Domain.Entities;

public sealed class Company : TenantOwnedEntity
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string LegalName { get; set; }
    public required string FunctionalCurrencyCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? CityRegion { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? TaxId { get; set; }
    public string? LogoUrl { get; set; }
}
