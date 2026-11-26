using Gasoft.Application.Abstractions;
using Gasoft.Application.Accounting;
using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Gasoft.Infrastructure.Tests;

public sealed class AccountingServiceTests
{
    [Fact]
    public async Task PostAsyncPostsBalancedEntryAndPersistsFunctionalTotals()
    {
        await using var db = CreateContext();
        await SeedEntryAsync(db, debit: 125m, credit: 125m);
        var service = new JournalEntryService(db, new CurrentTenantStub(12, 34));

        var result = await service.PostAsync(56, 78, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Posted", result.Status);
        Assert.Equal(125m, result.TotalDebit);
        Assert.Equal(125m, result.TotalCredit);
        var entry = await db.JournalEntries.SingleAsync(item => item.Id == 78);
        Assert.Equal("Posted", entry.Status);
        Assert.Equal(125m, entry.TotalDebit);
        Assert.Equal(125m, entry.TotalCredit);
    }

    [Fact]
    public async Task PostAsyncRejectsUnbalancedEntryAndLeavesItDraft()
    {
        await using var db = CreateContext();
        await SeedEntryAsync(db, debit: 125m, credit: 124m);
        var service = new JournalEntryService(db, new CurrentTenantStub(12, 34));

        await Assert.ThrowsAsync<AccountingValidationException>(
            () => service.PostAsync(56, 78, CancellationToken.None));

        var entry = await db.JournalEntries.SingleAsync(item => item.Id == 78);
        Assert.Equal("Draft", entry.Status);
        Assert.Equal(0m, entry.TotalDebit);
        Assert.Equal(0m, entry.TotalCredit);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options, new CurrentTenantStub(12, 34));
    }

    private static async Task SeedEntryAsync(AppDbContext db, decimal debit, decimal credit)
    {
        db.FiscalYears.Add(new FiscalYear
        {
            Id = 90,
            TenantId = 12,
            CompanyId = 56,
            StartsOn = new DateOnly(2026, 1, 1),
            EndsOn = new DateOnly(2026, 12, 31)
        });
        db.AccountingPeriods.Add(new AccountingPeriod
        {
            Id = 91,
            TenantId = 12,
            CompanyId = 56,
            FiscalYearId = 90,
            StartsOn = new DateOnly(2026, 1, 1),
            EndsOn = new DateOnly(2026, 12, 31)
        });
        db.JournalEntries.Add(new JournalEntry
        {
            Id = 78,
            TenantId = 12,
            CompanyId = 56,
            JournalId = 92,
            AccountingPeriodId = 91,
            EntryDate = new DateOnly(2026, 6, 15),
            Description = "Test journal entry",
            Lines =
            [
                new JournalLine
                {
                    TenantId = 12,
                    CompanyId = 56,
                    JournalEntryId = 78,
                    AccountId = 93,
                    TransactionCurrencyCode = "USD",
                    Debit = debit,
                    FunctionalDebit = debit,
                    ExchangeRate = 1
                },
                new JournalLine
                {
                    TenantId = 12,
                    CompanyId = 56,
                    JournalEntryId = 78,
                    AccountId = 94,
                    TransactionCurrencyCode = "USD",
                    Credit = credit,
                    FunctionalCredit = credit,
                    ExchangeRate = 1
                }
            ]
        });

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private sealed class CurrentTenantStub(long? tenantId, long? userId) : ICurrentTenant
    {
        public long? TenantId { get; } = tenantId;
        public long? UserId { get; } = userId;
    }
}
