namespace Gasoft.Domain.Entities;

public sealed class TaxCode
{
    public long Id { get; set; }
    public long JurisdictionId { get; set; }
    public long CategoryId { get; set; }
    public long? TenantId { get; set; }
    public long? CompanyId { get; set; }
    public required string Code { get; set; }
    public required string TaxType { get; set; }
}
