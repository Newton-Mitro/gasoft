namespace Gasoft.Application.Billing;

public static class SalesInvoiceBalance
{
    public static decimal CreditableAmount(decimal grandTotal, decimal creditedTotal) =>
        Math.Max(0, grandTotal - creditedTotal);

    public static decimal AllocatableAmount(
        decimal grandTotal,
        decimal allocatedTotal,
        decimal creditedTotal,
        decimal refundedTotal) =>
        Math.Max(0, grandTotal - allocatedTotal - creditedTotal + refundedTotal);

    public static decimal RefundableAmount(
        decimal creditNoteTotal,
        decimal noteRefundedTotal,
        decimal collectedAmount,
        decimal invoiceRefundedTotal) =>
        Math.Max(0, Math.Min(
            creditNoteTotal - noteRefundedTotal,
            collectedAmount - invoiceRefundedTotal));
}
