using Gasoft.Application.Abstractions;
using Gasoft.Application.Accounting;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Gasoft.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class AccountingService
{
    public async Task<IReadOnlyList<AccountGroupDto>> GetAccountGroupsAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        return await db.AccountGroups.AsNoTracking()
            .Where(group => group.CompanyId == companyId)
            .OrderBy(group => group.Code)
            .Select(group => new AccountGroupDto(
                group.Id, group.CompanyId, group.Code, group.Name, group.AccountType, group.NormalBalance))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedAccountGroupsDto> GetAccountGroupsPageAsync(
        long companyId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw Validation("pagination", "Page must be positive and page size must be between 1 and 100.");
        }

        var groups = db.AccountGroups.AsNoTracking().Where(group => group.CompanyId == companyId);
        var normalizedSearch = Normalize(search).ToLowerInvariant();
        if (normalizedSearch.Length > 0)
        {
            groups = groups.Where(group =>
                group.Code.ToLower().Contains(normalizedSearch) ||
                group.Name.ToLower().Contains(normalizedSearch) ||
                group.AccountType.ToLower().Contains(normalizedSearch));
        }

        var totalCount = await groups.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
        var items = await groups
            .OrderBy(group => group.Code)
            .ThenBy(group => group.Id)
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .Select(group => new AccountGroupDto(
                group.Id, group.CompanyId, group.Code, group.Name, group.AccountType, group.NormalBalance))
            .ToListAsync(cancellationToken);
        return new PagedAccountGroupsDto(items, normalizedPage, pageSize, totalCount, totalPages);
    }

    public async Task<AccountGroupDto?> GetAccountGroupAsync(
        long companyId,
        long groupId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var group = await db.AccountGroups.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == groupId, cancellationToken);
        return group is null ? null : ToDto(group);
    }

    public async Task<AccountGroupDto> CreateAccountGroupAsync(
        long companyId,
        CreateAccountGroupRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var code = Normalize(request.Code).ToUpperInvariant();
        var name = Normalize(request.Name);
        var type = Normalize(request.AccountType);
        var errors = new Dictionary<string, string[]>();
        ValidateCode(errors, "code", code, 30);
        ValidateName(errors, "name", name, 120);
        if (!AccountTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
        {
            errors["accountType"] = ["Account type must be Asset, Liability, Equity, Revenue, or Expense."];
        }

        ThrowIfInvalid(errors);
        var normalizedType = AccountTypes.Single(candidate => candidate.Equals(type, StringComparison.OrdinalIgnoreCase));
        var group = new AccountGroup
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = code,
            Name = name,
            AccountType = normalizedType,
            NormalBalance = normalizedType is "Asset" or "Expense" ? "Debit" : "Credit"
        };
        db.AccountGroups.Add(group);
        await SaveAsync(cancellationToken);
        return ToDto(group);
    }

    public async Task<AccountGroupDto?> UpdateAccountGroupAsync(
        long companyId,
        long groupId,
        UpdateAccountGroupRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var group = await db.AccountGroups.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == groupId,
            cancellationToken);
        if (group is null) return null;

        var code = Normalize(request.Code).ToUpperInvariant();
        var name = Normalize(request.Name);
        var type = Normalize(request.AccountType);
        var errors = new Dictionary<string, string[]>();
        ValidateCode(errors, "code", code, 30);
        ValidateName(errors, "name", name, 120);
        if (!AccountTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
        {
            errors["accountType"] = ["Account type must be Asset, Liability, Equity, Revenue, or Expense."];
        }
        ThrowIfInvalid(errors);

        var normalizedType = AccountTypes.Single(candidate => candidate.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (normalizedType != group.AccountType &&
            await db.ChartOfAccounts.AnyAsync(
                account => account.CompanyId == companyId && account.AccountGroupId == groupId,
                cancellationToken))
        {
            throw Validation("accountType", "Account type cannot be changed while accounts use this group.");
        }

        group.Code = code;
        group.Name = name;
        group.AccountType = normalizedType;
        group.NormalBalance = normalizedType is "Asset" or "Expense" ? "Debit" : "Credit";
        await SaveAsync(cancellationToken);
        return ToDto(group);
    }

    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(long companyId, CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        return await db.ChartOfAccounts.AsNoTracking()
            .Where(account => account.CompanyId == companyId)
            .OrderBy(account => account.Code)
            .Select(account => new AccountDto(
                account.Id,
                account.CompanyId,
                account.AccountGroupId,
                account.ParentId,
                account.Code,
                account.Name,
                account.AllowPosting,
                account.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedAccountsDto> GetAccountsPageAsync(
        long companyId,
        int page,
        int pageSize,
        long? accountGroupId,
        string? search,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw Validation("pagination", "Page must be positive and page size must be between 1 and 100.");
        }

        var roots = db.ChartOfAccounts.AsNoTracking()
            .Where(account => account.CompanyId == companyId && account.ParentId == null);
        if (accountGroupId is not null)
        {
            roots = roots.Where(account => account.AccountGroupId == accountGroupId);
        }

        var normalizedSearch = Normalize(search);
        if (normalizedSearch.Length > 0)
        {
            roots = roots.Where(account =>
                account.Code.ToLower().Contains(normalizedSearch.ToLower()) ||
                account.Name.ToLower().Contains(normalizedSearch.ToLower()));
        }

        var totalCount = await roots.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
        var pageRoots = await roots
            .OrderBy(account => account.Code)
            .ThenBy(account => account.Id)
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .Select(account => new AccountDto(
                account.Id,
                account.CompanyId,
                account.AccountGroupId,
                account.ParentId,
                account.Code,
                account.Name,
                account.AllowPosting,
                account.IsActive))
            .ToListAsync(cancellationToken);

        var items = new List<AccountDto>(pageRoots);
        var seenIds = pageRoots.Select(account => account.Id).ToHashSet();
        var parentIds = pageRoots.Select(account => account.Id).ToArray();
        while (parentIds.Length > 0)
        {
            var children = await db.ChartOfAccounts.AsNoTracking()
                .Where(account => account.CompanyId == companyId &&
                    account.ParentId.HasValue && parentIds.Contains(account.ParentId.Value))
                .OrderBy(account => account.Code)
                .ThenBy(account => account.Id)
                .Select(account => new AccountDto(
                    account.Id,
                    account.CompanyId,
                    account.AccountGroupId,
                    account.ParentId,
                    account.Code,
                    account.Name,
                    account.AllowPosting,
                    account.IsActive))
                .ToListAsync(cancellationToken);

            var unseenChildren = children.Where(account => seenIds.Add(account.Id)).ToList();
            items.AddRange(unseenChildren);
            parentIds = unseenChildren.Select(account => account.Id).ToArray();
        }

        var totalAccounts = await db.ChartOfAccounts.CountAsync(
            account => account.CompanyId == companyId,
            cancellationToken);
        return new PagedAccountsDto(items, normalizedPage, pageSize, totalCount, totalPages, totalAccounts);
    }

    public async Task<AccountDto?> GetAccountAsync(
        long companyId,
        long accountId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var account = await db.ChartOfAccounts.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.CompanyId == companyId && item.Id == accountId,
                cancellationToken);
        return account is null ? null : ToDto(account);
    }
}
