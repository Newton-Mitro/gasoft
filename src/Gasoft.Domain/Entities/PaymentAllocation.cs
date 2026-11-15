namespace Gasoft.Domain.Entities;

public sealed class PaymentAllocation : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long PaymentId { get; set; }
    public long? SalesInvoiceId { get; set; }
    public long? PurchaseBillId { get; set; }
    public decimal Amount { get; set; }
}
