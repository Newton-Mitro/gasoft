using Gasoft.Application.Abstractions;
using Gasoft.Application.Accounting;
using Gasoft.Application.Common;
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

      [Fact]
      public async Task PrepareInitialFiscalYearOpeningBalancesCreatesDraftWithEquityOffset()
      {
          await using var db = CreateContext();
          await SeedInitialOpeningBalancesAsync(db);
          var tenant = new CurrentTenantStub(12, 34);
          var service = new AccountingService(db, tenant, new JournalEntryService(db, tenant));

          var result = await service.PrepareInitialFiscalYearOpeningBalancesAsync(
              56,
              90,
              new PrepareInitialFiscalYearOpeningBalancesRequest(
              [
                  new InitialFiscalYearOpeningBalanceLineRequest(111, 1_000m, 0m),
                  new InitialFiscalYearOpeningBalanceLineRequest(112, 0m, 250m)
              ]),
              CancellationToken.None);

          Assert.NotNull(result);
          Assert.Equal("Draft", result.Status);
          var entry = await db.JournalEntries.Include(item => item.Lines).SingleAsync();
          Assert.Equal(new DateOnly(2026, 1, 1), entry.EntryDate);
          Assert.Equal(3, entry.Lines.Count);
          Assert.Equal(750m, entry.Lines.Single(line => line.AccountId == 113).Credit);
          Assert.Equal(
              entry.Lines.Sum(line => line.Debit),
              entry.Lines.Sum(line => line.Credit));
      }

      [Fact]
      public async Task PrepareInitialFiscalYearOpeningBalancesRejectsFiscalYearsWithPriorYears()
      {
          await using var db = CreateContext();
          await SeedInitialOpeningBalancesAsync(db);
          db.FiscalYears.Add(new FiscalYear
          {
              Id = 89,
              TenantId = 12,
              CompanyId = 56,
              StartsOn = new DateOnly(2025, 1, 1),
              EndsOn = new DateOnly(2025, 12, 31)
          });
          await db.SaveChangesAsync();
          var tenant = new CurrentTenantStub(12, 34);
          var service = new AccountingService(db, tenant, new JournalEntryService(db, tenant));

          var exception = await Assert.ThrowsAsync<AccountingConflictException>(
              () => service.PrepareInitialFiscalYearOpeningBalancesAsync(
                  56,
                  90,
                  new PrepareInitialFiscalYearOpeningBalancesRequest(
                  [
                      new InitialFiscalYearOpeningBalanceLineRequest(111, 1_000m, 0m)
                  ]),
                  CancellationToken.None));

          Assert.Contains("first fiscal year", exception.Message);
          Assert.Empty(await db.JournalEntries.ToListAsync());
      }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options, new CurrentTenantStub(12, 34));
    }

    private static async Task SeedInitialOpeningBalancesAsync(AppDbContext db)
    {
        db.Currencies.Add(new Currency { Code = "USD", Name = "US Dollar", DecimalPlaces = 2 });
        db.Companies.Add(new Company
        {
            Id = 56,
            TenantId = 12,
            Code = "TEST",
            LegalName = "Test company",
            FunctionalCurrencyCode = "USD"
        });
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
        db.Journals.Add(new Journal
        {
            Id = 92,
            TenantId = 12,
            CompanyId = 56,
            Code = "GEN",
            Name = "General journal",
            JournalType = "General"
        });
        db.AccountGroups.AddRange(
            new AccountGroup
            {
                Id = 101,
                TenantId = 12,
                CompanyId = 56,
                Code = "ASSET",
                Name = "Assets",
                AccountType = "Asset",
                NormalBalance = "Debit"
            },
            new AccountGroup
            {
                Id = 102,
                TenantId = 12,
                CompanyId = 56,
                Code = "LIABILITY",
                Name = "Liabilities",
                AccountType = "Liability",
                NormalBalance = "Credit"
            },
            new AccountGroup
            {
                Id = 103,
                TenantId = 12,
                CompanyId = 56,
                Code = "EQUITY",
                Name = "Equity",
                AccountType = "Equity",
                NormalBalance = "Credit"
            });
        db.ChartOfAccounts.AddRange(
            new ChartOfAccount
            {
                Id = 111,
                TenantId = 12,
                CompanyId = 56,
                AccountGroupId = 101,
                Code = "1000",
                Name = "Cash"
            },
            new ChartOfAccount
            {
                Id = 112,
                TenantId = 12,
                CompanyId = 56,
                AccountGroupId = 102,
                Code = "2000",
                Name = "Payables"
            },
            new ChartOfAccount
            {
                Id = 113,
                TenantId = 12,
                CompanyId = 56,
                AccountGroupId = 103,
                Code = "3000",
                Name = "Opening balance equity"
            });
        await db.SaveChangesAsync();
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
