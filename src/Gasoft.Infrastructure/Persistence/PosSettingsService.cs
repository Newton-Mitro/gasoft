using Gasoft.Application.Abstractions;
using Gasoft.Application.Pos;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Infrastructure.Persistence;

public sealed class PosSettingsService(AppDbContext db, ICurrentTenant currentTenant)
    : IPosSettingsService
{
    public async Task<PosSettingsDto?> GetAsync(long companyId, CancellationToken cancellationToken)
    {
        if (!await db.Companies.AnyAsync(company => company.Id == companyId, cancellationToken))
            return null;

        var settings = await db.PosSettings.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CompanyId == companyId, cancellationToken);
        return settings is null ? EmptySettings() : ToDto(settings);
    }

    public async Task<PosSettingsDto?> SaveAsync(
        long companyId,
        SavePosSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentTenant.TenantId;
        if (!tenantId.HasValue ||
            !await db.Companies.AnyAsync(company => company.Id == companyId, cancellationToken))
            return null;

        var errors = new Dictionary<string, string[]>();
        await ValidateReferenceAsync(
            errors, "warehouseId", request.WarehouseId,
            "Select a warehouse belonging to this company.",
            db.Warehouses.AnyAsync(item =>
                item.Id == request.WarehouseId && item.CompanyId == companyId, cancellationToken));
        await ValidateReferenceAsync(
            errors, "journalId", request.JournalId,
            "Select an active journal belonging to this company.",
            db.Journals.AnyAsync(item =>
                item.Id == request.JournalId && item.CompanyId == companyId && item.IsActive, cancellationToken));
        await ValidateReferenceAsync(
            errors, "accountingPeriodId", request.AccountingPeriodId,
            "Select an open accounting period belonging to this company.",
            db.AccountingPeriods.AnyAsync(item =>
                item.Id == request.AccountingPeriodId &&
                item.CompanyId == companyId &&
                item.Status == "Open", cancellationToken));
        await ValidateAccountAsync(
            errors, "revenueAccountId", request.RevenueAccountId, companyId, "Revenue", cancellationToken);
        await ValidateAccountAsync(
            errors, "receivableAccountId", request.ReceivableAccountId, companyId, "Asset", cancellationToken);
        await ValidateAccountAsync(
            errors, "settlementAccountId", request.SettlementAccountId, companyId, "Asset", cancellationToken);
        await ValidateAccountAsync(
            errors, "inventoryAccountId", request.InventoryAccountId, companyId, "Asset", cancellationToken);
        await ValidateAccountAsync(
            errors, "costOfGoodsSoldAccountId", request.CostOfGoodsSoldAccountId, companyId, "Expense", cancellationToken);

        if (request.TaxCodeId is not null)
        {
            if (request.TaxCodeId <= 0 ||
                !await db.TaxCodes.AnyAsync(code =>
                    code.Id == request.TaxCodeId &&
                    ((code.TenantId == null && code.CompanyId == null) ||
                     (code.TenantId == tenantId.Value && code.CompanyId == companyId)),
                    cancellationToken))
                errors["taxCodeId"] = ["Select a valid tax code for this company."];

            await ValidateAccountAsync(
                errors, "taxAccountId", request.TaxAccountId, companyId, "Liability", cancellationToken);
        }
        else if (request.TaxAccountId is not null)
        {
            errors["taxAccountId"] = ["Choose a tax code before selecting a tax liability account."];
        }

        if (errors.Count != 0) throw new PosSettingsValidationException(errors);

        var settings = await db.PosSettings
            .SingleOrDefaultAsync(item => item.CompanyId == companyId, cancellationToken);
        if (settings is null)
        {
            settings = new PosSettings { TenantId = tenantId.Value, CompanyId = companyId };
            db.PosSettings.Add(settings);
        }

        settings.WarehouseId = request.WarehouseId!.Value;
        settings.JournalId = request.JournalId!.Value;
        settings.AccountingPeriodId = request.AccountingPeriodId!.Value;
        settings.RevenueAccountId = request.RevenueAccountId!.Value;
        settings.ReceivableAccountId = request.ReceivableAccountId!.Value;
        settings.SettlementAccountId = request.SettlementAccountId!.Value;
        settings.TaxCodeId = request.TaxCodeId;
        settings.TaxAccountId = request.TaxAccountId;
        settings.InventoryAccountId = request.InventoryAccountId!.Value;
        settings.CostOfGoodsSoldAccountId = request.CostOfGoodsSoldAccountId!.Value;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(settings);
    }

    private async Task ValidateAccountAsync(
        IDictionary<string, string[]> errors,
        string field,
        long? accountId,
        long companyId,
        string accountType,
        CancellationToken cancellationToken)
    {
        if (accountId is null or <= 0 ||
            !await (
                from account in db.ChartOfAccounts
                join accountGroup in db.AccountGroups on new
                {
                    account.TenantId,
                    account.CompanyId,
                    Id = account.AccountGroupId
                } equals new
                {
                    accountGroup.TenantId,
                    accountGroup.CompanyId,
                    accountGroup.Id
                }
                where account.Id == accountId &&
                      account.CompanyId == companyId &&
                      account.IsActive &&
                      account.AllowPosting &&
                      accountGroup.AccountType == accountType
                select account.Id
            ).AnyAsync(cancellationToken))
        {
            errors[field] = [$"Select an active, postable {accountType.ToLowerInvariant()} account."];
        }
    }

    private static async Task ValidateReferenceAsync(
        IDictionary<string, string[]> errors,
        string field,
        long? value,
        string message,
        Task<bool> exists)
    {
        if (value is null or <= 0 || !await exists)
            errors[field] = [message];
    }

    private static PosSettingsDto EmptySettings() => new(
        null, null, null, null, null, null, null, null, null, null);

    private static PosSettingsDto ToDto(PosSettings settings) => new(
        settings.WarehouseId,
        settings.JournalId,
        settings.AccountingPeriodId,
        settings.RevenueAccountId,
        settings.ReceivableAccountId,
        settings.SettlementAccountId,
        settings.TaxCodeId,
        settings.TaxAccountId,
        settings.InventoryAccountId,
        settings.CostOfGoodsSoldAccountId);
}
