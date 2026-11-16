namespace Gasoft.Domain.Entities;

public sealed class BankReconciliationMatch : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long BankAccountId { get; set; }
    public long BankStatementTransactionId { get; set; }
    public long JournalLineId { get; set; }
    public decimal Amount { get; set; }
}
