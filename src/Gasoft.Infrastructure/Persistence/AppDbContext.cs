using Gasoft.Application.Abstractions;
using Gasoft.Domain.Entities;
using Gasoft.Domain.Notifications;
using Gasoft.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Gasoft.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentTenant currentTenant)
    : IdentityDbContext<ApplicationUser, IdentityRole<long>, long>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();
    public DbSet<NotificationEmailOutbox> NotificationEmailOutbox => Set<NotificationEmailOutbox>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<TenantEmailConfiguration> TenantEmailConfigurations => Set<TenantEmailConfiguration>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<PosSettings> PosSettings => Set<PosSettings>();
    public DbSet<CompanyUser> CompanyUsers => Set<CompanyUser>();
    public DbSet<Party> Parties => Set<Party>();
    public DbSet<PartyAddress> PartyAddresses => Set<PartyAddress>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<TaxJurisdiction> TaxJurisdictions => Set<TaxJurisdiction>();
    public DbSet<TaxCategory> TaxCategories => Set<TaxCategory>();
    public DbSet<TaxCode> TaxCodes => Set<TaxCode>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<TenantTaxRegistration> TenantTaxRegistrations => Set<TenantTaxRegistration>();
    public DbSet<AccountGroup> AccountGroups => Set<AccountGroup>();
    public DbSet<ChartOfAccount> ChartOfAccounts => Set<ChartOfAccount>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<FiscalYearStatusChange> FiscalYearStatusChanges => Set<FiscalYearStatusChange>();
    public DbSet<FiscalYearClosing> FiscalYearClosings => Set<FiscalYearClosing>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();
    public DbSet<AccountingPeriodStatusChange> AccountingPeriodStatusChanges => Set<AccountingPeriodStatusChange>();
    public DbSet<Journal> Journals => Set<Journal>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<FixedAsset> FixedAssets => Set<FixedAsset>();
    public DbSet<FixedAssetDepreciation> FixedAssetDepreciations => Set<FixedAssetDepreciation>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceLine> SalesInvoiceLines => Set<SalesInvoiceLine>();
    public DbSet<SalesQuote> SalesQuotes => Set<SalesQuote>();
    public DbSet<SalesQuoteLine> SalesQuoteLines => Set<SalesQuoteLine>();
    public DbSet<SalesCreditNote> SalesCreditNotes => Set<SalesCreditNote>();
    public DbSet<SalesCreditNoteLine> SalesCreditNoteLines => Set<SalesCreditNoteLine>();
    public DbSet<CustomerRefund> CustomerRefunds => Set<CustomerRefund>();
    public DbSet<PurchaseBill> PurchaseBills => Set<PurchaseBill>();
    public DbSet<PurchaseBillLine> PurchaseBillLines => Set<PurchaseBillLine>();
    public DbSet<ExpenseReport> ExpenseReports => Set<ExpenseReport>();
    public DbSet<ExpenseReportLine> ExpenseReportLines => Set<ExpenseReportLine>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<CompanyItemPrice> CompanyItemPrices => Set<CompanyItemPrice>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<InventoryValuationLayer> InventoryValuationLayers => Set<InventoryValuationLayer>();
    public DbSet<InventoryCount> InventoryCounts => Set<InventoryCount>();
    public DbSet<InventoryCountLine> InventoryCountLines => Set<InventoryCountLine>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankStatementTransaction> BankStatementTransactions => Set<BankStatementTransaction>();
    public DbSet<BankReconciliationMatch> BankReconciliationMatches => Set<BankReconciliationMatch>();
    public DbSet<ConsolidationGroup> ConsolidationGroups => Set<ConsolidationGroup>();
    public DbSet<ConsolidationGroupCompany> ConsolidationGroupCompanies => Set<ConsolidationGroupCompany>();
    public DbSet<ConsolidationRun> ConsolidationRuns => Set<ConsolidationRun>();
    public DbSet<ConsolidationJournalLine> ConsolidationJournalLines => Set<ConsolidationJournalLine>();
    public DbSet<ConsolidationTranslationRate> ConsolidationTranslationRates => Set<ConsolidationTranslationRate>();
    public long? CurrentTenantId => currentTenant.TenantId;
    public long? CurrentUserId => currentTenant.UserId;

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var pendingAudits = CaptureAuditEntries();
        if (pendingAudits.Count == 0)
        {
            var notifications = CapturePendingNotifications();
            QueueNotificationEmails(notifications);
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        using var transaction = Database.CurrentTransaction is null
            ? Database.BeginTransaction()
            : null;
        var entriesNormalized = false;
        try
        {
            QueueNotificationEmails(CapturePendingNotifications());
            var result = base.SaveChanges(acceptAllChangesOnSuccess);
            if (!acceptAllChangesOnSuccess)
            {
                NormalizeEntriesForAuditSave(pendingAudits);
                entriesNormalized = true;
            }
            PersistAuditEntries(pendingAudits);
            var notifications = CapturePendingNotifications();
            QueueNotificationEmails(notifications);
            base.SaveChanges();
            if (!acceptAllChangesOnSuccess)
                RestoreEntriesAfterAuditSave(pendingAudits);
            transaction?.Commit();
            return result;
        }
        catch
        {
            transaction?.Rollback();
            if (entriesNormalized)
                RestoreEntriesAfterAuditSave(pendingAudits);
            throw;
        }
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        var pendingAudits = CaptureAuditEntries();
        if (pendingAudits.Count == 0)
        {
            var notifications = CapturePendingNotifications();
            QueueNotificationEmails(notifications);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        await using var transaction = Database.CurrentTransaction is null
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;
        var entriesNormalized = false;
        try
        {
            QueueNotificationEmails(CapturePendingNotifications());
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            if (!acceptAllChangesOnSuccess)
            {
                NormalizeEntriesForAuditSave(pendingAudits);
                entriesNormalized = true;
            }
            await PersistAuditEntriesAsync(pendingAudits, cancellationToken);
            var notifications = CapturePendingNotifications();
            QueueNotificationEmails(notifications);
            await base.SaveChangesAsync(cancellationToken);
            if (!acceptAllChangesOnSuccess)
                RestoreEntriesAfterAuditSave(pendingAudits);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            if (entriesNormalized)
                RestoreEntriesAfterAuditSave(pendingAudits);
            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppNotification>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(40).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(100).HasDefaultValue("legacy").IsRequired();
            entity.Property(x => x.Link).HasMaxLength(500);
            entity.HasIndex(x => new { x.TenantId, x.UserId, x.DismissedAt, x.CreatedAt });
            entity.HasQueryFilter(x =>
                CurrentTenantId.HasValue &&
                x.TenantId == CurrentTenantId.Value &&
                CurrentUserId.HasValue &&
                x.UserId == CurrentUserId.Value);
        });

        builder.Entity<NotificationEmailOutbox>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.NextAttemptAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.LeaseExpiresAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.SentAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.LastError).HasMaxLength(2000);
            entity.HasIndex(x => new { x.TenantId, x.NotificationId }).IsUnique();
            entity.HasIndex(x => new { x.Status, x.NextAttemptAt, x.LeaseExpiresAt });
            entity.HasQueryFilter(x =>
                CurrentTenantId.HasValue &&
                x.TenantId == CurrentTenantId.Value &&
                CurrentUserId.HasValue &&
                x.UserId == CurrentUserId.Value);
            entity.HasOne(x => x.Notification)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.NotificationId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_NotificationEmailOutbox_Status",
                    "\"Status\" IN ('Pending', 'Processing', 'Sent', 'Failed')");
                table.HasCheckConstraint(
                    "CK_NotificationEmailOutbox_Attempts",
                    "\"AttemptCount\" >= 0");
            });
        });

        builder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.UserId, x.EventType });
            entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(x =>
                CurrentTenantId.HasValue &&
                x.TenantId == CurrentTenantId.Value &&
                CurrentUserId.HasValue &&
                x.UserId == CurrentUserId.Value);
        });

        builder.Entity<TenantEmailConfiguration>(entity =>
        {
            entity.HasKey(x => x.TenantId);
            entity.Property(x => x.SmtpHost).HasMaxLength(253).IsRequired();
            entity.Property(x => x.SmtpUsername).HasMaxLength(200);
            entity.Property(x => x.ProtectedSmtpPassword).HasMaxLength(8192);
            entity.Property(x => x.FromEmail).HasMaxLength(320).IsRequired();
            entity.Property(x => x.FromName).HasMaxLength(200).IsRequired();
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(40).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(200);
            entity.Property(x => x.DetailsJson).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.CreatedAt });
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<Tenant>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.Id == CurrentTenantId.Value);
        });

        builder.Entity<TenantUser>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.UserId });
            entity.Property(x => x.Role).HasMaxLength(40).IsRequired();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(200);
            entity.Property(x => x.ProfileImageUrl).HasMaxLength(500);
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
        });

        builder.Entity<Company>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.LegalName).HasMaxLength(250).IsRequired();
            entity.Property(x => x.FunctionalCurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Website).HasMaxLength(2000);
            entity.Property(x => x.Address).HasMaxLength(250);
            entity.Property(x => x.CityRegion).HasMaxLength(150);
            entity.Property(x => x.PostalCode).HasMaxLength(30);
            entity.Property(x => x.Country).HasMaxLength(100);
            entity.Property(x => x.TaxId).HasMaxLength(100);
            entity.Property(x => x.LogoUrl).HasMaxLength(500);
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.FunctionalCurrencyCode).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<CompanyUser>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.CompanyId, x.UserId });
            entity.Property(x => x.Role).HasMaxLength(40).IsRequired();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TenantUser>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.UserId })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<PosSettings>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.CompanyId }).IsUnique();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<Party>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.PartyNo }).IsUnique();
            entity.Property(x => x.PartyNo).HasMaxLength(40).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.DefaultCurrencyCode).HasMaxLength(3).IsFixedLength();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.DefaultCurrencyCode).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<PartyAddress>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.PartyId });
            entity.Property(x => x.AddressType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Line1).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Line2).HasMaxLength(200);
            entity.Property(x => x.City).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Region).HasMaxLength(100);
            entity.Property(x => x.PostalCode).HasMaxLength(20);
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsFixedLength().IsRequired();
            entity.HasOne<Party>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.PartyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<Currency>(entity =>
        {
            entity.HasKey(x => x.Code);
            entity.Property(x => x.Code).HasMaxLength(3).IsFixedLength();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.ToTable(table => table.HasCheckConstraint("CK_Currencies_DecimalPlaces", "\"DecimalPlaces\" <= 4"));
            entity.HasData(
                new Currency { Code = "AUD", Name = "Australian Dollar", DecimalPlaces = 2 },
                new Currency { Code = "BDT", Name = "Bangladeshi Taka", DecimalPlaces = 2 },
                new Currency { Code = "CAD", Name = "Canadian Dollar", DecimalPlaces = 2 },
                new Currency { Code = "CHF", Name = "Swiss Franc", DecimalPlaces = 2 },
                new Currency { Code = "EUR", Name = "Euro", DecimalPlaces = 2 },
                new Currency { Code = "GBP", Name = "Pound Sterling", DecimalPlaces = 2 },
                new Currency { Code = "INR", Name = "Indian Rupee", DecimalPlaces = 2 },
                new Currency { Code = "JPY", Name = "Japanese Yen", DecimalPlaces = 0 },
                new Currency { Code = "SGD", Name = "Singapore Dollar", DecimalPlaces = 2 },
                new Currency { Code = "USD", Name = "US Dollar", DecimalPlaces = 2 });
        });

        builder.Entity<TaxJurisdiction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsFixedLength().IsRequired();
            entity.HasData(TaxJurisdictionSeed.All);
        });

        builder.Entity<TaxCategory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
        });

        builder.Entity<TaxCode>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.JurisdictionId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
            entity.Property(x => x.TaxType).HasMaxLength(40).IsRequired();
            entity.HasOne<TaxJurisdiction>().WithMany().HasForeignKey(x => x.JurisdictionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TaxCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_TaxCodes_CompanyScope",
                "(\"TenantId\" IS NULL AND \"CompanyId\" IS NULL) OR (\"TenantId\" IS NOT NULL AND \"CompanyId\" IS NOT NULL)"));
        });

        builder.Entity<TaxRate>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RatePercent).HasPrecision(9, 6);
            entity.HasOne<TaxCode>().WithMany().HasForeignKey(x => x.TaxCodeId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_TaxRates_RatePercent", "\"RatePercent\" >= 0 AND \"RatePercent\" <= 100"));
        });

        builder.Entity<TenantTaxRegistration>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.JurisdictionId }).IsUnique();
            entity.Property(x => x.RegistrationNumber).HasMaxLength(100).IsRequired();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TaxJurisdiction>().WithMany().HasForeignKey(x => x.JurisdictionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<AccountGroup>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AccountType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.NormalBalance).HasMaxLength(10).IsRequired();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<ChartOfAccount>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CashFlowCategory).HasMaxLength(32);
            entity.HasOne<AccountGroup>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.AccountGroupId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.ParentId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<FiscalYear>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.StartsOn }).IsUnique();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_FiscalYears_DateRange", "\"EndsOn\" >= \"StartsOn\""));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<AccountingPeriod>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.FiscalYearId, x.StartsOn }).IsUnique();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.HasOne<FiscalYear>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.FiscalYearId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_AccountingPeriods_DateRange", "\"EndsOn\" >= \"StartsOn\""));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<FiscalYearStatusChange>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FromStatus).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ToStatus).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.FiscalYearId, x.ChangedAt });
            entity.HasOne<FiscalYear>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.FiscalYearId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<FiscalYearClosing>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.FiscalYearId }).IsUnique();
            entity.HasOne<FiscalYear>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.FiscalYearId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalEntry>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.JournalEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.RetainedEarningsAccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<AccountingPeriodStatusChange>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FromStatus).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ToStatus).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.AccountingPeriodId, x.ChangedAt });
            entity.HasOne<AccountingPeriod>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.AccountingPeriodId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<Journal>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.JournalType).HasMaxLength(20).IsRequired();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.TotalDebit).HasPrecision(20, 6);
            entity.Property(x => x.TotalCredit).HasPrecision(20, 6);
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.ReversalOfEntryId })
                .IsUnique()
                .HasDatabaseName("UX_JournalEntries_TenantId_CompanyId_ReversalOfEntryId");
            entity.HasOne<Journal>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.JournalId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalEntry>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.ReversalOfEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AccountingPeriod>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.AccountingPeriodId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<JournalLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.Property(x => x.TransactionCurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.Debit).HasPrecision(20, 6);
            entity.Property(x => x.Credit).HasPrecision(20, 6);
            entity.Property(x => x.FunctionalDebit).HasPrecision(20, 6);
            entity.Property(x => x.FunctionalCredit).HasPrecision(20, 6);
            entity.Property(x => x.ExchangeRate).HasPrecision(20, 10);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasOne<JournalEntry>()
                .WithMany(x => x.Lines)
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.JournalEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, AccountId = x.AccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, Id = x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CostCenter>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.CostCenterId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>()
                .WithMany()
                .HasForeignKey(x => x.TransactionCurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_JournalLines_Amounts",
                "\"Debit\" >= 0 AND \"Credit\" >= 0 AND ((\"Debit\" > 0 AND \"Credit\" = 0) OR (\"Credit\" > 0 AND \"Debit\" = 0))"));
        });

        builder.Entity<CostCenter>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<Budget>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.FiscalYearId, x.Name }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.HasOne<FiscalYear>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.FiscalYearId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<BudgetLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.BudgetId, x.AccountingPeriodId, x.AccountId }).IsUnique();
            entity.Property(x => x.Amount).HasPrecision(20, 6);
            entity.HasOne<Budget>()
                .WithMany(x => x.Lines)
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.BudgetId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<AccountingPeriod>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.AccountingPeriodId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.AccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_BudgetLines_Amount", "\"Amount\" >= 0"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<FixedAsset>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.AssetNumber }).IsUnique();
            entity.Property(x => x.AssetNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.AcquisitionCost).HasPrecision(20, 6);
            entity.Property(x => x.ResidualValue).HasPrecision(20, 6);
            entity.Property(x => x.DisposalProceeds).HasPrecision(20, 6);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.AssetAccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.AccumulatedDepreciationAccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.DepreciationExpenseAccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalEntry>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, JournalEntryId = x.AcquisitionJournalEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalEntry>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, JournalEntryId = x.DisposalJournalEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_FixedAssets_Amounts",
                "\"AcquisitionCost\" > 0 AND \"ResidualValue\" >= 0 AND \"ResidualValue\" <= \"AcquisitionCost\" AND \"UsefulLifeMonths\" > 0"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<FixedAssetDepreciation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.FixedAssetId, x.AccountingPeriodId }).IsUnique();
            entity.Property(x => x.Amount).HasPrecision(20, 6);
            entity.HasOne<FixedAsset>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.FixedAssetId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<AccountingPeriod>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.AccountingPeriodId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalEntry>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.JournalEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_FixedAssetDepreciations_Amount", "\"Amount\" > 0"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<ConsolidationGroup>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ReportingCurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>()
                .WithMany()
                .HasForeignKey(x => x.ReportingCurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<ConsolidationGroupCompany>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.ConsolidationGroupId, x.CompanyId });
            entity.HasOne<ConsolidationGroup>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ConsolidationGroupId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<ConsolidationRun>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.ConsolidationGroupId, x.PeriodEnd }).IsUnique();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.HasOne<ConsolidationGroup>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ConsolidationGroupId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<ConsolidationJournalLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountCode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.AccountName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Debit).HasPrecision(20, 6);
            entity.Property(x => x.Credit).HasPrecision(20, 6);
            entity.Property(x => x.ExchangeRate).HasPrecision(20, 10);
            entity.Property(x => x.Description).HasMaxLength(200);
            entity.HasOne<ConsolidationRun>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ConsolidationRunId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_ConsolidationJournalLines_Amounts",
                "\"Debit\" >= 0 AND \"Credit\" >= 0 AND ((\"Debit\" > 0 AND \"Credit\" = 0) OR (\"Credit\" > 0 AND \"Debit\" = 0))"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<ConsolidationTranslationRate>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.ConsolidationRunId, x.CompanyId }).IsUnique();
            entity.Property(x => x.SourceCurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.TargetCurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.ClosingRate).HasPrecision(20, 10);
            entity.Property(x => x.AverageRate).HasPrecision(20, 10);
            entity.HasOne<ConsolidationRun>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ConsolidationRunId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.SourceCurrencyCode).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.TargetCurrencyCode).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_ConsolidationTranslationRates_Rates",
                "\"ClosingRate\" > 0 AND \"AverageRate\" > 0"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<ExchangeRate>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.SourceCurrencyCode, x.TargetCurrencyCode, x.EffectiveOn }).IsUnique();
            entity.Property(x => x.SourceCurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.TargetCurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.Rate).HasPrecision(20, 10);
            entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.SourceCurrencyCode).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.TargetCurrencyCode).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_ExchangeRates_Rate", "\"Rate\" > 0"));
        });

        builder.Entity<BankAccount>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.ChartOfAccountId }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.BankName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AccountNumberLastFour).HasMaxLength(4).IsFixedLength().IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.ChartOfAccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, Id = x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<BankStatementTransaction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.BankAccountId, x.ImportKey }).IsUnique();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(120);
            entity.Property(x => x.Amount).HasPrecision(20, 6);
            entity.Property(x => x.ImportKey).HasMaxLength(64).IsFixedLength().IsRequired();
            entity.HasOne<BankAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.BankAccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, Id = x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table => table.HasCheckConstraint("CK_BankStatementTransactions_Amount", "\"Amount\" <> 0"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<BankReconciliationMatch>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.BankStatementTransactionId, x.JournalLineId }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.JournalLineId });
            entity.Property(x => x.Amount).HasPrecision(20, 6);
            entity.HasOne<BankStatementTransaction>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.BankStatementTransactionId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<BankAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.BankAccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalLine>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.JournalLineId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_BankReconciliationMatches_Amount",
                "\"Amount\" <> 0"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<SalesInvoice>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.InvoiceNumber }).IsUnique();
            entity.Property(x => x.InvoiceNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.ExchangeRate).HasPrecision(20, 10);
            entity.Property(x => x.NetTotal).HasPrecision(20, 6);
            entity.Property(x => x.TaxTotal).HasPrecision(20, 6);
            entity.Property(x => x.GrandTotal).HasPrecision(20, 6);
            entity.Property(x => x.AllocatedTotal).HasPrecision(20, 6);
            entity.Property(x => x.CreditedTotal).HasPrecision(20, 6);
            entity.Property(x => x.RefundedTotal).HasPrecision(20, 6);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Party>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CustomerId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalEntry>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.JournalEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SalesInvoices_Totals",
                "\"NetTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"GrandTotal\" = \"NetTotal\" + \"TaxTotal\" AND \"AllocatedTotal\" >= 0 AND \"AllocatedTotal\" <= \"GrandTotal\" AND \"CreditedTotal\" >= 0 AND \"CreditedTotal\" <= \"GrandTotal\" AND \"RefundedTotal\" >= 0 AND \"RefundedTotal\" <= \"CreditedTotal\""));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<SalesInvoiceLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(20, 6);
            entity.Property(x => x.UnitPrice).HasPrecision(20, 6);
            entity.Property(x => x.NetAmount).HasPrecision(20, 6);
            entity.Property(x => x.TaxRatePercent).HasPrecision(9, 6);
            entity.Property(x => x.TaxAmount).HasPrecision(20, 6);
            entity.Property(x => x.GrossAmount).HasPrecision(20, 6);
            entity.HasOne<SalesInvoice>()
                .WithMany(x => x.Lines)
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.SalesInvoiceId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, AccountId = x.AccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, Id = x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TaxCode>().WithMany().HasForeignKey(x => x.TaxCodeId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SalesInvoiceLines_Amounts",
                "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"NetAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"GrossAmount\" = \"NetAmount\" + \"TaxAmount\""));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<SalesCreditNote>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.CreditNoteNumber }).IsUnique();
            entity.Property(x => x.CreditNoteNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.NetTotal).HasPrecision(20, 6);
            entity.Property(x => x.TaxTotal).HasPrecision(20, 6);
            entity.Property(x => x.GrandTotal).HasPrecision(20, 6);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.ReceivableAccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, Id = x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SalesInvoice)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.SalesInvoiceId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.JournalEntry)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.JournalEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SalesCreditNotes_Totals",
                "\"NetTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"GrandTotal\" = \"NetTotal\" + \"TaxTotal\""));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<CustomerRefund>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.RefundNumber }).IsUnique();
            entity.Property(x => x.RefundNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(20, 6);
            entity.Property(x => x.FunctionalAmount).HasPrecision(20, 6);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.HasOne<SalesCreditNote>()
                .WithMany(x => x.Refunds)
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.SalesCreditNoteId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.JournalEntry)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.JournalEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_CustomerRefunds_Amounts",
                "\"Amount\" > 0 AND \"FunctionalAmount\" > 0"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<SalesCreditNoteLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(20, 6);
            entity.Property(x => x.UnitPrice).HasPrecision(20, 6);
            entity.Property(x => x.NetAmount).HasPrecision(20, 6);
            entity.Property(x => x.TaxRatePercent).HasPrecision(9, 6);
            entity.Property(x => x.TaxAmount).HasPrecision(20, 6);
            entity.Property(x => x.GrossAmount).HasPrecision(20, 6);
            entity.HasOne<SalesCreditNote>()
                .WithMany(x => x.Lines)
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.SalesCreditNoteId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<SalesInvoiceLine>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, SalesInvoiceId = x.SalesInvoiceLineId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, Id = x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, AccountId = x.AccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, Id = x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TaxCode>().WithMany().HasForeignKey(x => x.TaxCodeId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SalesCreditNoteLines_Amounts",
                "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"NetAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"GrossAmount\" = \"NetAmount\" + \"TaxAmount\""));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<SalesQuote>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.QuoteNumber }).IsUnique();
            entity.Property(x => x.QuoteNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.ExchangeRate).HasPrecision(20, 10);
            entity.Property(x => x.NetTotal).HasPrecision(20, 6);
            entity.Property(x => x.TaxTotal).HasPrecision(20, 6);
            entity.Property(x => x.GrandTotal).HasPrecision(20, 6);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.SentTo).HasMaxLength(320);
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Party>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CustomerId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SalesInvoice>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.SalesInvoiceId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SalesQuotes_Totals",
                "\"NetTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"GrandTotal\" = \"NetTotal\" + \"TaxTotal\""));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<SalesQuoteLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(20, 6);
            entity.Property(x => x.UnitPrice).HasPrecision(20, 6);
            entity.Property(x => x.NetAmount).HasPrecision(20, 6);
            entity.Property(x => x.TaxRatePercent).HasPrecision(9, 6);
            entity.Property(x => x.TaxAmount).HasPrecision(20, 6);
            entity.Property(x => x.GrossAmount).HasPrecision(20, 6);
            entity.HasOne<SalesQuote>()
                .WithMany(x => x.Lines)
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.SalesQuoteId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, AccountId = x.AccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, Id = x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TaxCode>().WithMany().HasForeignKey(x => x.TaxCodeId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SalesQuoteLines_Amounts",
                "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"NetAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"GrossAmount\" = \"NetAmount\" + \"TaxAmount\""));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<PurchaseBill>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.BillNumber }).IsUnique();
            entity.Property(x => x.BillNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.ExchangeRate).HasPrecision(20, 10);
            entity.Property(x => x.NetTotal).HasPrecision(20, 6);
            entity.Property(x => x.TaxTotal).HasPrecision(20, 6);
            entity.Property(x => x.GrandTotal).HasPrecision(20, 6);
            entity.Property(x => x.AllocatedTotal).HasPrecision(20, 6);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Party>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SupplierId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalEntry>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.JournalEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_PurchaseBills_Totals",
                "\"NetTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"GrandTotal\" = \"NetTotal\" + \"TaxTotal\" AND \"AllocatedTotal\" >= 0 AND \"AllocatedTotal\" <= \"GrandTotal\""));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<PurchaseBillLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(20, 6);
            entity.Property(x => x.UnitPrice).HasPrecision(20, 6);
            entity.Property(x => x.NetAmount).HasPrecision(20, 6);
            entity.Property(x => x.TaxRatePercent).HasPrecision(9, 6);
            entity.Property(x => x.TaxAmount).HasPrecision(20, 6);
            entity.Property(x => x.GrossAmount).HasPrecision(20, 6);
            entity.HasOne<PurchaseBill>()
                .WithMany(x => x.Lines)
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.PurchaseBillId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, AccountId = x.AccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, Id = x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TaxCode>().WithMany().HasForeignKey(x => x.TaxCodeId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_PurchaseBillLines_Amounts",
                "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"NetAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"GrossAmount\" = \"NetAmount\" + \"TaxAmount\""));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<ExpenseReport>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.ReportNumber }).IsUnique();
            entity.Property(x => x.ReportNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(20, 6);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ReviewNote).HasMaxLength(1000);
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalEntry>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.JournalEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalEntry>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.ReimbursementJournalEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.EmployeePayableAccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.ReimbursementCashAccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>()
                .WithMany()
                .HasForeignKey(x => x.CurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_ExpenseReports_TotalAmount",
                "\"TotalAmount\" > 0"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<ExpenseReportLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Merchant).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(20, 6);
            entity.Property(x => x.ReceiptFileName).HasMaxLength(100);
            entity.HasOne(x => x.ExpenseReport)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.ExpenseReportId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.AccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_ExpenseReportLines_Amount",
                "\"Amount\" > 0"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.OrderNumber }).IsUnique();
            entity.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.ExchangeRate).HasPrecision(20, 10);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Party>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SupplierId })
                .HasPrincipalKey(x => new { x.TenantId, Id = x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.WarehouseId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, Id = x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_PurchaseOrders_ExchangeRate",
                "\"ExchangeRate\" > 0"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(20, 6);
            entity.Property(x => x.ReceivedQuantity).HasPrecision(20, 6);
            entity.Property(x => x.UnitCost).HasPrecision(20, 6);
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasOne<PurchaseOrder>()
                .WithMany(x => x.Lines)
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.PurchaseOrderId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Item>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, ItemId = x.ItemId })
                .HasPrincipalKey(x => new { x.TenantId, Id = x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_PurchaseOrderLines_Quantities",
                "\"Quantity\" > 0 AND \"ReceivedQuantity\" >= 0 AND \"ReceivedQuantity\" <= \"Quantity\" AND \"UnitCost\" > 0"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.PaymentNumber }).IsUnique();
            entity.Property(x => x.PaymentNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Direction).HasMaxLength(10).IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.ExchangeRate).HasPrecision(20, 10);
            entity.Property(x => x.Amount).HasPrecision(20, 6);
            entity.Property(x => x.AllocatedAmount).HasPrecision(20, 6);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Party>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.PartyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalEntry>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.JournalEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Payments_Amounts",
                "\"Amount\" > 0 AND \"AllocatedAmount\" >= 0 AND \"AllocatedAmount\" <= \"Amount\""));
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Payments_Direction",
                "\"Direction\" IN ('Receive', 'Pay')"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<PaymentAllocation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(20, 6);
            entity.HasOne<Payment>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.PaymentId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<SalesInvoice>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.SalesInvoiceId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PurchaseBill>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.PurchaseBillId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_PaymentAllocations_Target",
                "(\"SalesInvoiceId\" IS NOT NULL AND \"PurchaseBillId\" IS NULL) OR (\"SalesInvoiceId\" IS NULL AND \"PurchaseBillId\" IS NOT NULL)"));
            entity.ToTable(table => table.HasCheckConstraint("CK_PaymentAllocations_Amount", "\"Amount\" > 0"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<UnitOfMeasure>(entity =>
        {
            entity.HasKey(x => x.Code);
            entity.Property(x => x.Code).HasMaxLength(12).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.ToTable(table => table.HasCheckConstraint("CK_UnitsOfMeasure_DecimalPlaces", "\"DecimalPlaces\" <= 6"));
            entity.HasData(
                new UnitOfMeasure { Code = "EA", Name = "Each", DecimalPlaces = 0 },
                new UnitOfMeasure { Code = "BOX", Name = "Box", DecimalPlaces = 0 },
                new UnitOfMeasure { Code = "HOUR", Name = "Hour", DecimalPlaces = 2 },
                new UnitOfMeasure { Code = "KG", Name = "Kilogram", DecimalPlaces = 3 },
                new UnitOfMeasure { Code = "L", Name = "Litre", DecimalPlaces = 3 },
                new UnitOfMeasure { Code = "M", Name = "Metre", DecimalPlaces = 3 });
        });

        builder.Entity<ItemCategory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<Item>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique();
            entity.Property(x => x.Sku).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CostingMethod).HasMaxLength(20).IsRequired();
            entity.HasOne<ItemCategory>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CategoryId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<UnitOfMeasure>().WithMany().HasForeignKey(x => x.BaseUnitCode).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<CompanyItemPrice>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.CompanyId, x.ItemId });
            entity.Property(x => x.UnitSellingPrice).HasPrecision(20, 6);
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_CompanyItemPrices_UnitSellingPrice",
                "\"UnitSellingPrice\" IS NULL OR \"UnitSellingPrice\" >= 0"));
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Item)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ItemId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<WarehouseLocation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.WarehouseId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.WarehouseId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.WarehouseId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.MovementNumber }).IsUnique();
            entity.Property(x => x.MovementNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.MovementType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.QuantityIn).HasPrecision(20, 6);
            entity.Property(x => x.QuantityOut).HasPrecision(20, 6);
            entity.Property(x => x.UnitCost).HasPrecision(20, 6);
            entity.Property(x => x.TotalCost).HasPrecision(20, 6);
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasOne<Item>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ItemId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.WarehouseId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<WarehouseLocation>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.WarehouseId, x.WarehouseLocationId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.WarehouseId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalEntry>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.JournalEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PurchaseOrderLine>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.PurchaseOrderLineId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_StockMovements_Quantities",
                "\"QuantityIn\" >= 0 AND \"QuantityOut\" >= 0 AND ((\"QuantityIn\" > 0 AND \"QuantityOut\" = 0) OR (\"QuantityOut\" > 0 AND \"QuantityIn\" = 0))"));
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_StockMovements_Cost",
                "\"UnitCost\" >= 0 AND \"TotalCost\" >= 0"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<InventoryValuationLayer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RemainingQuantity).HasPrecision(20, 6);
            entity.Property(x => x.UnitCost).HasPrecision(20, 6);
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasOne<StockMovement>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, StockMovementId = x.StockMovementId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, Id = x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Item>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ItemId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.WarehouseId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_InventoryValuationLayers_Amounts",
                "\"RemainingQuantity\" >= 0 AND \"UnitCost\" >= 0"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<InventoryCount>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.CountNumber }).IsUnique();
            entity.Property(x => x.CountNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.WarehouseId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalEntry>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, x.JournalEntryId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });

        builder.Entity<InventoryCountLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SystemQuantity).HasPrecision(20, 6);
            entity.Property(x => x.SystemValue).HasPrecision(20, 6);
            entity.Property(x => x.CountedQuantity).HasPrecision(20, 6);
            entity.Property(x => x.AdjustmentUnitCost).HasPrecision(20, 6);
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasOne<InventoryCount>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompanyId, InventoryCountId = x.InventoryCountId })
                .HasPrincipalKey(x => new { x.TenantId, x.CompanyId, Id = x.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Item>()
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.ItemId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_InventoryCountLines_Quantities",
                "\"SystemQuantity\" >= 0 AND \"SystemValue\" >= 0 AND \"CountedQuantity\" >= 0 AND (\"AdjustmentUnitCost\" IS NULL OR \"AdjustmentUnitCost\" > 0)"));
            entity.HasQueryFilter(x => CurrentTenantId.HasValue && x.TenantId == CurrentTenantId.Value);
        });
    }

    private List<PendingAuditEntry> CaptureAuditEntries()
    {
        ChangeTracker.DetectChanges();
        return ChangeTracker.Entries()
            .Where(entry =>
                entry.Entity is not AuditLog &&
                entry.Entity is not AppNotification &&
                entry.Entity is not NotificationPreference &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                entry.Metadata.FindProperty(nameof(TenantOwnedEntity.TenantId)) is not null)
            .Select(entry =>
            {
                var tenantValue = entry.Property(nameof(TenantOwnedEntity.TenantId)).CurrentValue;
                var tenantId = tenantValue is long value ? value : (long?)null;
                if (!tenantId.HasValue || tenantId != CurrentTenantId)
                    return null;

                var entityType = entry.Metadata.ClrType.Name;
                var modifiedProperties = entry.Properties
                    .Where(property => property.IsModified)
                    .Select(property => property.Metadata.Name)
                    .ToArray();
                var statusChange = entityType switch
                {
                    nameof(FiscalYearStatusChange) => ("Fiscal year", "FiscalYearId"),
                    nameof(AccountingPeriodStatusChange) => ("Accounting period", "AccountingPeriodId"),
                    _ => ((string Name, string IdProperty)?)null
                };
                var action = statusChange.HasValue
                    ? "StatusChanged"
                    : entry.State == EntityState.Modified &&
                      entry.Entity is Company &&
                      modifiedProperties.Length > 0 &&
                      modifiedProperties.All(property => property == nameof(Company.LogoUrl))
                        ? "LogoUpdated"
                        : entry.State switch
                        {
                            EntityState.Added => "Created",
                            EntityState.Modified => "Updated",
                            _ => "Deleted"
                        };
                var changes = entry.Properties
                    .Where(property => !IsSensitiveProperty(property.Metadata.Name))
                    .Where(property => entry.State != EntityState.Modified || property.IsModified)
                    .ToDictionary(
                        property => property.Metadata.Name,
                        property => entry.State == EntityState.Added
                            ? new { oldValue = (object?)null, newValue = AuditValue(property.CurrentValue) }
                            : entry.State == EntityState.Deleted
                                ? new { oldValue = AuditValue(property.OriginalValue), newValue = (object?)null }
                                : new { oldValue = AuditValue(property.OriginalValue), newValue = AuditValue(property.CurrentValue) });
                var key = entry.Metadata.FindPrimaryKey();
                var entityId = statusChange.HasValue
                    ? entry.Property(statusChange.Value.IdProperty).CurrentValue?.ToString()
                    : key is null
                        ? null
                        : string.Join(
                            ":",
                            key.Properties.Select(property => entry.Property(property.Name).CurrentValue?.ToString() ?? string.Empty));
                var entityName = statusChange?.Name ?? entityType;
                var statusProperty = entry.Properties.FirstOrDefault(property =>
                    property.Metadata.Name.Equals("Status", StringComparison.OrdinalIgnoreCase));
                var statusValue = statusChange.HasValue
                    ? entry.Property("ToStatus").CurrentValue?.ToString()
                    : statusProperty?.CurrentValue?.ToString();
                var statusChanged = statusChange.HasValue ||
                    statusProperty is not null &&
                    (entry.State == EntityState.Added || statusProperty.IsModified);
                var postsRecord = statusChanged &&
                    statusValue?.Equals("Posted", StringComparison.OrdinalIgnoreCase) == true;
                var journalEntry = entry.Entity as JournalEntry;
                var notificationEventType = statusChanged || entry.State == EntityState.Added
                    ? NotificationEvents.ResolveWorkflowEventType(
                        entityType,
                        statusValue,
                        journalEntry?.Description,
                        journalEntry?.ReversalOfEntryId,
                        entry.State == EntityState.Added)
                    : null;
                var notificationDefinition = notificationEventType is null
                    ? null
                    : NotificationEvents.Find(notificationEventType);
                var auditAction = notificationEventType ??
                    (postsRecord ? "Posted" : action);
                if (notificationEventType is null &&
                    entityType == nameof(BankStatementTransaction) &&
                    entry.State == EntityState.Added)
                    auditAction = NotificationEvents.BankStatementImported;
                var notificationTitle = notificationDefinition?.Name;
                var notificationMessage = postsRecord
                    ? journalEntry?.ReversalOfEntryId is long originalEntryId
                        ? $"Journal entry #{originalEntryId} was reversed on {journalEntry.EntryDate:yyyy-MM-dd} with debit and credit amounts swapped."
                        : journalEntry is not null
                            ? $"Journal entry #{journalEntry.Id} \"{journalEntry.Description}\" was posted on {journalEntry.EntryDate:yyyy-MM-dd}."
                            : notificationDefinition?.Description
                    : notificationDefinition?.Description;
                return new PendingAuditEntry(
                    entry.Entity,
                    entry.State,
                    entry.Properties.Where(property => property.IsModified)
                        .Select(property => property.Metadata.Name)
                        .ToArray(),
                    entry.Properties.ToDictionary(
                        property => property.Metadata.Name,
                        property => property.OriginalValue),
                    tenantId.Value,
                    currentTenant.UserId,
                    auditAction,
                    entityName,
                    entityId,
                    JsonSerializer.Serialize(changes),
                    notificationTitle,
                    notificationMessage,
                    notificationEventType);
            })
            .Where(entry => entry is not null)
            .Select(entry => entry!)
            .ToList();
    }

    private void PersistAuditEntries(IReadOnlyCollection<PendingAuditEntry> pendingAudits)
    {
        if (pendingAudits.Count == 0) return;
        AddAuditEntries(pendingAudits);
        AddWorkflowNotifications(pendingAudits);
    }

    private async Task PersistAuditEntriesAsync(
        IReadOnlyCollection<PendingAuditEntry> pendingAudits,
        CancellationToken cancellationToken)
    {
        if (pendingAudits.Count == 0) return;
        AddAuditEntries(pendingAudits);
        await AddWorkflowNotificationsAsync(pendingAudits, cancellationToken);
    }

    private void AddAuditEntries(IEnumerable<PendingAuditEntry> pendingAudits)
    {
        foreach (var pending in pendingAudits)
        {
            var entry = Entry(pending.Entity);
            var primaryKey = entry.Metadata.FindPrimaryKey();
            var entityId = primaryKey is null
                ? pending.EntityId
                : string.Join(":", primaryKey.Properties.Select(property =>
                    entry.Property(property.Name).CurrentValue?.ToString() ?? string.Empty));
            AuditLogs.Add(new AuditLog
            {
                TenantId = pending.TenantId,
                ActorUserId = pending.ActorUserId,
                Action = pending.Action,
                EntityType = pending.EntityType,
                EntityId = entityId,
                DetailsJson = pending.DetailsJson,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private void NormalizeEntriesForAuditSave(IEnumerable<PendingAuditEntry> pendingAudits)
    {
        foreach (var pending in pendingAudits)
        {
            var entry = Entry(pending.Entity);
            entry.State = pending.State == EntityState.Deleted
                ? EntityState.Detached
                : EntityState.Unchanged;
        }
    }

    private void RestoreEntriesAfterAuditSave(IEnumerable<PendingAuditEntry> pendingAudits)
    {
        foreach (var pending in pendingAudits)
        {
            var entry = Entry(pending.Entity);
            if (pending.State == EntityState.Deleted)
            {
                entry.State = EntityState.Deleted;
                RestoreOriginalValues(entry, pending.OriginalValues);
            }
            else if (pending.State == EntityState.Added)
            {
                entry.State = EntityState.Added;
            }
            else
            {
                entry.State = EntityState.Unchanged;
                RestoreOriginalValues(entry, pending.OriginalValues);
                foreach (var property in pending.ModifiedProperties)
                    entry.Property(property).IsModified = true;
            }
        }
    }

    private static void RestoreOriginalValues(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        IReadOnlyDictionary<string, object?> originalValues)
    {
        foreach (var (name, value) in originalValues)
            entry.Property(name).OriginalValue = value;
    }

    private void AddWorkflowNotifications(IEnumerable<PendingAuditEntry> pendingAudits)
    {
        var workflowEvents = pendingAudits
            .Where(entry => entry.NotificationTitle is not null && entry.ActorUserId.HasValue)
            .ToArray();
        foreach (var group in workflowEvents.GroupBy(entry => entry.TenantId))
        {
            var recipientIds = TenantUsers.IgnoreQueryFilters()
                .Where(user => user.TenantId == group.Key &&
                    user.IsActive &&
                    (user.Role == "Owner" || user.Role == "Admin") &&
                    user.UserId != currentTenant.UserId)
                .Select(user => user.UserId)
                .ToArray();
            AddNotifications(group, recipientIds);
        }
    }

    private async Task AddWorkflowNotificationsAsync(
        IEnumerable<PendingAuditEntry> pendingAudits,
        CancellationToken cancellationToken)
    {
        var workflowEvents = pendingAudits
            .Where(entry => entry.NotificationTitle is not null && entry.ActorUserId.HasValue)
            .ToArray();
        foreach (var group in workflowEvents.GroupBy(entry => entry.TenantId))
        {
            var recipientIds = await TenantUsers.IgnoreQueryFilters()
                .Where(user => user.TenantId == group.Key &&
                    user.IsActive &&
                    (user.Role == "Owner" || user.Role == "Admin") &&
                    user.UserId != currentTenant.UserId)
                .Select(user => user.UserId)
                .ToListAsync(cancellationToken);
            AddNotifications(group, recipientIds);
        }
    }

    private void AddNotifications(IEnumerable<PendingAuditEntry> workflowEvents, IEnumerable<long> recipientIds)
    {
        var events = workflowEvents.ToArray();
        var recipients = recipientIds.Distinct().ToArray();
        if (events.Length == 0 || recipients.Length == 0) return;
        foreach (var workflowEvent in events)
            foreach (var recipientId in recipients)
            {
                if (workflowEvent.NotificationEventType is not string eventType) continue;
                Notifications.Add(new AppNotification
                {
                    TenantId = workflowEvent.TenantId,
                    UserId = recipientId,
                    Title = workflowEvent.NotificationTitle!,
                    Message = workflowEvent.NotificationMessage!,
                    Category = "workflow",
                    EventType = eventType,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
    }

    private AppNotification[] CapturePendingNotifications() =>
        ChangeTracker.Entries<AppNotification>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToArray();

    private void QueueNotificationEmails(IEnumerable<AppNotification> notifications)
    {
        var queuedNotifications = ChangeTracker.Entries<NotificationEmailOutbox>()
            .Select(entry => entry.Reference(outbox => outbox.Notification).CurrentValue)
            .OfType<AppNotification>()
            .ToHashSet(ReferenceEqualityComparer.Instance);
        foreach (var notification in notifications.Distinct())
        {
            if (queuedNotifications.Contains(notification)) continue;
            NotificationEmailOutbox.Add(new NotificationEmailOutbox
            {
                TenantId = notification.TenantId,
                NotificationId = notification.Id,
                UserId = notification.UserId,
                EventType = notification.EventType,
                Title = notification.Title,
                Message = notification.Message,
                NextAttemptAt = DateTimeOffset.UtcNow,
                Notification = notification
            });
        }
    }

    private static bool IsSensitiveProperty(string name) =>
        name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("credential", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("accountnumber", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("iban", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("routingnumber", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("cardnumber", StringComparison.OrdinalIgnoreCase);

    private static object? AuditValue(object? value)
    {
        if (value is null) return null;
        if (value is byte[] || value is Stream) return "[binary]";
        return value is string text && text.Length > 512 ? text[..512] : value;
    }

    private sealed record PendingAuditEntry(
        object Entity,
        EntityState State,
        string[] ModifiedProperties,
        IReadOnlyDictionary<string, object?> OriginalValues,
        long TenantId,
        long? ActorUserId,
        string Action,
        string EntityType,
        string? EntityId,
        string DetailsJson,
        string? NotificationTitle,
        string? NotificationMessage,
        string? NotificationEventType);
}
