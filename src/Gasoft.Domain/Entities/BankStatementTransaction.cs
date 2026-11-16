namespace Gasoft.Domain.Entities;

public sealed class BankStatementTransaction : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long BankAccountId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public required string Description { get; set; }
    public string? Reference { get; set; }
    public decimal Amount { get; set; }
    public required string ImportKey { get; set; }
}
