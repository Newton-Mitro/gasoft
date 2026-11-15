namespace Gasoft.Domain.Entities;

public sealed class PartyAddress : TenantOwnedEntity
{
    public long Id { get; set; }
    public long PartyId { get; set; }
    public required string AddressType { get; set; }
    public required string Line1 { get; set; }
    public string? Line2 { get; set; }
    public required string City { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public required string CountryCode { get; set; }
    public bool IsDefault { get; set; }
}
