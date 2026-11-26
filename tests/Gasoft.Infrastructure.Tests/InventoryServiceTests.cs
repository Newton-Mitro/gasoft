using Gasoft.Application.Abstractions;
using Gasoft.Application.Inventory;
using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Gasoft.Infrastructure.Tests;

public sealed class InventoryServiceTests
{
    [Fact]
    public async Task ReceiveAsyncCreatesMovementValuationLayerAndBalancedJournalEntry()
    {
        await using var db = CreateContext();
        await SeedReceiptDependenciesAsync(db);
        db.ChangeTracker.Clear();
        var service = new StockMovementService(db, new CurrentTenantStub(12, 34));

        var result = await service.ReceiveAsync(
            56,
            new ReceiveStockRequest(
                "RCV-001",
                71,
                72,
                null,
                73,
                74,
                77,
                78,
                new DateOnly(2026, 6, 15),
                3,
                10),
            CancellationToken.None);

        Assert.Equal(71, result.ItemId);
        Assert.Equal(72, result.WarehouseId);
        Assert.Equal(3m, result.QuantityIn);
        Assert.Equal(10m, result.UnitCost);
        Assert.Equal(30m, result.TotalCost);

        var layer = await db.InventoryValuationLayers.SingleAsync();
        Assert.Equal(result.Id, layer.StockMovementId);
        Assert.Equal(3m, layer.RemainingQuantity);
        Assert.Equal(10m, layer.UnitCost);

        var entry = await db.JournalEntries.Include(item => item.Lines).SingleAsync();
        Assert.Equal("Posted", entry.Status);
        Assert.Equal(30m, entry.TotalDebit);
        Assert.Equal(30m, entry.TotalCredit);
        Assert.Equal(30m, entry.Lines.Sum(line => line.FunctionalDebit));
        Assert.Equal(30m, entry.Lines.Sum(line => line.FunctionalCredit));

        var balances = await new InventoryService(
                db,
                new CurrentTenantStub(12, 34),
                new StockMovementService(db, new CurrentTenantStub(12, 34)))
            .GetStockBalancesPageAsync(56, 1, 25, CancellationToken.None);
        var balance = Assert.Single(balances.Items);
        Assert.Equal("Test item", balance.ItemName);
        Assert.Equal("Main", balance.WarehouseName);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options, new CurrentTenantStub(12, 34));
    }

    private static async Task SeedReceiptDependenciesAsync(AppDbContext db)
    {
        db.AddRange(
            new Company
            {
                Id = 56,
                TenantId = 12,
                Code = "TEST",
                LegalName = "Test Company",
                FunctionalCurrencyCode = "USD"
            },
            new Item
            {
                Id = 71,
                TenantId = 12,
                CategoryId = 70,
                BaseUnitCode = "EA",
                Sku = "TEST-ITEM",
                Name = "Test item",
                CostingMethod = "FIFO"
            },
            new Warehouse { Id = 72, TenantId = 12, CompanyId = 56, Code = "MAIN", Name = "Main" },
            new Journal { Id = 73, TenantId = 12, CompanyId = 56, Code = "INV", Name = "Inventory", JournalType = "General" },
            new AccountingPeriod
            {
                Id = 74,
                TenantId = 12,
                CompanyId = 56,
                FiscalYearId = 75,
                StartsOn = new DateOnly(2026, 1, 1),
                EndsOn = new DateOnly(2026, 12, 31)
            },
            new AccountGroup
            {
                Id = 76,
                TenantId = 12,
                CompanyId = 56,
                Code = "ASSET",
                Name = "Assets",
                AccountType = "Asset",
                NormalBalance = "Debit"
            },
            new AccountGroup
            {
                Id = 79,
                TenantId = 12,
                CompanyId = 56,
                Code = "LIABILITY",
                Name = "Liabilities",
                AccountType = "Liability",
                NormalBalance = "Credit"
            },
            new ChartOfAccount
            {
                Id = 77,
                TenantId = 12,
                CompanyId = 56,
                AccountGroupId = 76,
                Code = "1200",
                Name = "Inventory"
            },
            new ChartOfAccount
            {
                Id = 78,
                TenantId = 12,
                CompanyId = 56,
                AccountGroupId = 79,
                Code = "2100",
                Name = "Stock clearing"
            },
            new Currency { Code = "USD", Name = "US Dollar", DecimalPlaces = 2 });

        await db.SaveChangesAsync();
    }

    private sealed class CurrentTenantStub(long? tenantId, long? userId) : ICurrentTenant
    {
        public long? TenantId { get; } = tenantId;
        public long? UserId { get; } = userId;
    }
}
