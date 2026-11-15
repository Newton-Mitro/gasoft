namespace Gasoft.Domain.Entities;

public sealed class Party : TenantOwnedEntity
{
    public long Id { get; set; }
    public required string PartyNo { get; set; }
    public required string DisplayName { get; set; }
    public bool IsCustomer { get; set; }
    public bool IsSupplier { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? DefaultCurrencyCode { get; set; }
}
