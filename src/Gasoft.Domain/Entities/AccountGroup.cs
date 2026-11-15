namespace Gasoft.Domain.Entities;

public sealed class AccountGroup : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string AccountType { get; set; }
    public required string NormalBalance { get; set; }
}
