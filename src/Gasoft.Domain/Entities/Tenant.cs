namespace Gasoft.Domain.Entities;

public sealed class Tenant
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string Status { get; set; } = "active";
}
