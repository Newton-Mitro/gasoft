using Gasoft.Application.Billing;

namespace Gasoft.Infrastructure.Tests;

public sealed class SalesInvoiceBalanceTests
{
    [Fact]
    public void CreditableAmountDoesNotDependOnPaymentsAlreadyAllocated()
    {
        Assert.Equal(100m, SalesInvoiceBalance.CreditableAmount(100m, 0m));
    }

    [Fact]
    public void CreditableAmountCannotExceedUncreditedInvoiceTotal()
    {
        Assert.Equal(35m, SalesInvoiceBalance.CreditableAmount(100m, 65m));
    }

    [Fact]
    public void AllocatableAmountIncludesCreditsAndRefunds()
    {
        Assert.Equal(20m, SalesInvoiceBalance.AllocatableAmount(100m, 40m, 50m, 10m));
    }

    [Fact]
    public void RefundableAmountUsesTheSmallerNoteAndInvoiceCashBalance()
    {
        Assert.Equal(20m, SalesInvoiceBalance.RefundableAmount(50m, 10m, 100m, 80m));
    }

    [Fact]
    public void RefundableAmountNeverGoesBelowZero()
    {
        Assert.Equal(0m, SalesInvoiceBalance.RefundableAmount(50m, 10m, 25m, 30m));
    }
}
