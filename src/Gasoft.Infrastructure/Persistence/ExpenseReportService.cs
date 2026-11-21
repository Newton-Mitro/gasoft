using Gasoft.Application.Abstractions;
using Gasoft.Application.Accounting;
using Gasoft.Application.Expenses;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Infrastructure.Persistence;

public sealed class ExpenseReportService(
    AppDbContext db,
    ICurrentTenant currentTenant,
    IAccountingService accounting) : IExpenseReportService
{
    private const decimal MaximumAmount = 99_999_999_999_999m;

    public async Task<IReadOnlyList<ExpenseReportDto>> GetReportsAsync(
        long companyId,
        bool includeAll,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAccessAsync(companyId, cancellationToken);
        var userId = EnsureUser();
        var query = db.ExpenseReports
            .AsNoTracking()
            .Include(report => report.Lines)
            .Where(report => report.CompanyId == companyId);
        if (!includeAll)
            query = query.Where(report => report.EmployeeUserId == userId);

        var reports = await query
            .OrderByDescending(report => report.Id)
            .ToListAsync(cancellationToken);
        var names = await GetEmployeeNamesAsync(reports.Select(report => report.EmployeeUserId), cancellationToken);
        return reports.Select(report => ToDto(
                report,
                names.GetValueOrDefault(report.EmployeeUserId) ?? "Employee",
                includeAll,
                userId))
            .ToArray();
    }

    public async Task<ExpenseReportDto?> GetReportAsync(
        long companyId,
        long reportId,
        bool includeAll,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAccessAsync(companyId, cancellationToken);
        var userId = EnsureUser();
        var report = await db.ExpenseReports
            .AsNoTracking()
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == reportId, cancellationToken);
        if (report is null || (!includeAll && report.EmployeeUserId != userId))
            return null;

        var employeeName = await GetEmployeeNameAsync(report.EmployeeUserId, cancellationToken);
        return ToDto(report, employeeName, includeAll, userId);
    }

    public async Task<ExpenseReportDto> CreateReportAsync(
        long companyId,
        CreateExpenseReportRequest request,
        CancellationToken cancellationToken)
    {
        var company = await EnsureCompanyAccessAsync(companyId, cancellationToken);
        var userId = EnsureUser();
        if (request.Lines.Any(line => line.Id.HasValue))
            throw Validation("lines", "New expense report lines cannot include identifiers.");
        var normalized = await ValidateRequestAsync(company, request, cancellationToken);
        var report = new ExpenseReport
        {
            TenantId = company.TenantId,
            CompanyId = companyId,
            EmployeeUserId = userId,
            ReportNumber = CreateReportNumber(),
            Title = Normalize(request.Title),
            CurrencyCode = company.FunctionalCurrencyCode,
            TotalAmount = normalized.Sum(line => line.Amount),
            Revision = 1
        };
        foreach (var line in normalized)
            report.Lines.Add(ToEntity(company.TenantId, companyId, line));

        db.ExpenseReports.Add(report);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(
            report,
            await GetEmployeeNameAsync(userId, cancellationToken),
            includeAll: false,
            userId);
    }

    public async Task<ExpenseReportUpdateResult?> UpdateReportAsync(
        long companyId,
        long reportId,
        CreateExpenseReportRequest request,
        CancellationToken cancellationToken)
    {
        var company = await EnsureCompanyAccessAsync(companyId, cancellationToken);
        var userId = EnsureUser();
        var report = await GetTrackedReportAsync(companyId, reportId, cancellationToken);
        if (report is null)
            return null;
        EnsureEditable(report, userId);

        var normalized = await ValidateRequestAsync(company, request, cancellationToken);
        var existingLines = report.Lines.ToDictionary(line => line.Id);
        var requestedIds = normalized.Where(line => line.Id.HasValue).Select(line => line.Id!.Value).ToArray();
        if (requestedIds.Distinct().Count() != requestedIds.Length ||
            requestedIds.Any(id => !existingLines.ContainsKey(id)))
        {
            throw Validation("lines", "Each existing line must belong to this expense report and appear only once.");
        }

        var removedLines = report.Lines.Where(line => !requestedIds.Contains(line.Id)).ToArray();
        var removedReceipts = removedLines
            .Where(line => line.ReceiptFileName is not null)
            .Select(line => new ExpenseReceiptReference(report.Id, line.Id, line.ReceiptFileName!))
            .ToArray();
        db.ExpenseReportLines.RemoveRange(removedLines);
        foreach (var removedLine in removedLines)
            report.Lines.Remove(removedLine);

        report.Title = Normalize(request.Title);
        report.TotalAmount = normalized.Sum(line => line.Amount);
        report.Revision++;
        foreach (var line in normalized)
        {
            if (line.Id is long existingId)
            {
                var existing = existingLines[existingId];
                existing.ExpenseDate = line.ExpenseDate;
                existing.Merchant = line.Merchant;
                existing.Description = line.Description;
                existing.AccountId = line.AccountId;
                existing.Amount = line.Amount;
            }
            else
            {
                report.Lines.Add(ToEntity(company.TenantId, companyId, line));
            }
        }

        await SaveWithConcurrencyHandlingAsync(cancellationToken);
        return new ExpenseReportUpdateResult(
            ToDto(
                report,
                await GetEmployeeNameAsync(userId, cancellationToken),
                includeAll: false,
                userId),
            removedReceipts);
    }

    public async Task<ExpenseReportDto?> SubmitReportAsync(
        long companyId,
        long reportId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAccessAsync(companyId, cancellationToken);
        var userId = EnsureUser();
        var report = await GetTrackedReportAsync(companyId, reportId, cancellationToken);
        if (report is null)
            return null;
        EnsureEditable(report, userId);
        if (report.Lines.Count == 0)
            throw Validation("lines", "Add at least one expense before submitting the report.");

        report.Status = "Submitted";
        report.SubmittedAt = DateTimeOffset.UtcNow;
        report.ReviewerUserId = null;
        report.ReviewedAt = null;
        report.ReviewNote = null;
        report.Revision++;
        await SaveWithConcurrencyHandlingAsync(cancellationToken);
        return await ToDtoAsync(report, includeAll: false, userId, cancellationToken);
    }

    public async Task<ExpenseReportDto?> ReviewReportAsync(
        long companyId,
        long reportId,
        bool approve,
        ReviewExpenseReportRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAccessAsync(companyId, cancellationToken);
        var userId = EnsureUser();
        var report = await GetTrackedReportAsync(companyId, reportId, cancellationToken);
        if (report is null)
            return null;
        if (report.Status != "Submitted")
            throw new ExpenseReportConflictException("Only submitted expense reports can be reviewed.");

        var note = NormalizeNullable(request.Note);
        if (!approve && (note is null || note.Length > 1000))
            throw Validation("note", "Enter a rejection reason of 1–1000 characters.");
        if (approve && note?.Length > 1000)
            throw Validation("note", "The review note must be at most 1000 characters.");

        report.Status = approve ? "Approved" : "Rejected";
        report.ReviewerUserId = userId;
        report.ReviewedAt = DateTimeOffset.UtcNow;
        report.ReviewNote = note;
        report.Revision++;
        await SaveWithConcurrencyHandlingAsync(cancellationToken);
        return await ToDtoAsync(report, includeAll: true, userId, cancellationToken);
    }

    public async Task<ExpenseReportDto?> PostReportAsync(
        long companyId,
        long reportId,
        PostExpenseReportRequest request,
        CancellationToken cancellationToken)
    {
        var company = await EnsureCompanyAccessAsync(companyId, cancellationToken);
        var userId = EnsureUser();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var report = await GetTrackedReportAsync(companyId, reportId, cancellationToken);
        if (report is null)
            return null;
        if (report.Status != "Approved")
            throw new ExpenseReportConflictException("Only approved expense reports can be posted.");

        await EnsureExpenseLineAccountsAsync(companyId, report.Lines, cancellationToken);
        await EnsureAccountTypeAsync(companyId, request.EmployeePayableAccountId, "Liability", cancellationToken);
        var lines = report.Lines
            .Select(line => new CreateJournalLineRequest(
                line.AccountId,
                company.FunctionalCurrencyCode,
                line.Amount,
                0,
                1,
                line.Description))
            .Append(new CreateJournalLineRequest(
                request.EmployeePayableAccountId,
                company.FunctionalCurrencyCode,
                0,
                report.TotalAmount,
                1,
                $"Employee payable for {report.ReportNumber}"))
            .ToArray();
        var journal = await accounting.CreateDraftAsync(
            companyId,
            new CreateJournalEntryRequest(
                request.JournalId,
                request.AccountingPeriodId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                $"Expense report {report.ReportNumber}",
                lines),
            cancellationToken);
        if (journal is null)
            return null;
        var postedJournal = await accounting.PostAsync(companyId, journal.Id, cancellationToken);
        if (postedJournal is null)
            throw new ExpenseReportConflictException("The generated expense journal entry could not be posted.");

        report.Status = "Posted";
        report.JournalEntryId = postedJournal.Id;
        report.EmployeePayableAccountId = request.EmployeePayableAccountId;
        report.PostedAt = DateTimeOffset.UtcNow;
        report.Revision++;
        await SaveWithConcurrencyHandlingAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await ToDtoAsync(report, includeAll: true, userId, cancellationToken);
    }

    public async Task<ExpenseReportDto?> ReimburseReportAsync(
        long companyId,
        long reportId,
        ReimburseExpenseReportRequest request,
        CancellationToken cancellationToken)
    {
        var company = await EnsureCompanyAccessAsync(companyId, cancellationToken);
        var userId = EnsureUser();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var report = await GetTrackedReportAsync(companyId, reportId, cancellationToken);
        if (report is null)
            return null;
        if (report.Status != "Posted" || report.EmployeePayableAccountId is null)
            throw new ExpenseReportConflictException("Only posted expense reports can be reimbursed.");

        await EnsureAccountTypeAsync(companyId, report.EmployeePayableAccountId.Value, "Liability", cancellationToken);
        await EnsureAccountTypeAsync(companyId, request.CashAccountId, "Asset", cancellationToken);
        var journal = await accounting.CreateDraftAsync(
            companyId,
            new CreateJournalEntryRequest(
                request.JournalId,
                request.AccountingPeriodId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                $"Reimbursement for {report.ReportNumber}",
                [
                    new CreateJournalLineRequest(
                        report.EmployeePayableAccountId.Value,
                        company.FunctionalCurrencyCode,
                        report.TotalAmount,
                        0,
                        1,
                        $"Reimburse employee for {report.ReportNumber}"),
                    new CreateJournalLineRequest(
                        request.CashAccountId,
                        company.FunctionalCurrencyCode,
                        0,
                        report.TotalAmount,
                        1,
                        $"Pay expense report {report.ReportNumber}")
                ]),
            cancellationToken);
        if (journal is null)
            return null;
        var postedJournal = await accounting.PostAsync(companyId, journal.Id, cancellationToken);
        if (postedJournal is null)
            throw new ExpenseReportConflictException("The generated reimbursement journal entry could not be posted.");

        report.Status = "Reimbursed";
        report.ReimbursementJournalEntryId = postedJournal.Id;
        report.ReimbursementCashAccountId = request.CashAccountId;
        report.ReimbursedAt = DateTimeOffset.UtcNow;
        report.Revision++;
        await SaveWithConcurrencyHandlingAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await ToDtoAsync(report, includeAll: true, userId, cancellationToken);
    }

    public async Task<string?> GetReceiptFileNameAsync(
        long companyId,
        long reportId,
        long lineId,
        bool includeAll,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAccessAsync(companyId, cancellationToken);
        var userId = EnsureUser();
        return await db.ExpenseReportLines
            .Where(line =>
                line.CompanyId == companyId &&
                line.ExpenseReportId == reportId &&
                line.Id == lineId &&
                (line.ExpenseReport.EmployeeUserId == userId || includeAll))
            .Select(line => line.ReceiptFileName)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ExpenseReportDto?> SetReceiptAsync(
        long companyId,
        long reportId,
        long lineId,
        string fileName,
        bool includeAll,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAccessAsync(companyId, cancellationToken);
        var userId = EnsureUser();
        var report = await GetTrackedReportAsync(companyId, reportId, cancellationToken);
        if (report is null)
            return null;
        EnsureEditable(report, userId);
        var line = report.Lines.SingleOrDefault(item => item.Id == lineId);
        if (line is null)
            return null;

        line.ReceiptFileName = fileName;
        report.Revision++;
        await SaveWithConcurrencyHandlingAsync(cancellationToken);
        return await ToDtoAsync(report, includeAll, userId, cancellationToken);
    }

    private async Task<Company> EnsureCompanyAccessAsync(long companyId, CancellationToken cancellationToken)
    {
        var tenantId = currentTenant.TenantId
            ?? throw new UnauthorizedAccessException("A tenant-scoped access token is required.");
        EnsureUser();
        var company = await db.Companies.SingleOrDefaultAsync(
            item => item.Id == companyId && item.TenantId == tenantId,
            cancellationToken);
        if (company is null)
            throw new UnauthorizedAccessException("The company is not available in the current tenant.");

        return company;
    }

    private async Task<List<ValidatedLine>> ValidateRequestAsync(
        Company company,
        CreateExpenseReportRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var title = Normalize(request.Title);
        if (title.Length is 0 or > 200)
            AddError(errors, "title", "A title of 1–200 characters is required.");
        if (request.Lines is null || request.Lines.Count is < 1 or > 100)
            AddError(errors, "lines", "An expense report must contain 1–100 expense lines.");

        var lines = request.Lines ?? [];
        var currency = await db.Currencies.SingleOrDefaultAsync(
            item => item.Code == company.FunctionalCurrencyCode,
            cancellationToken);
        if (currency is null)
            throw new ExpenseReportConflictException("The company's functional currency is not configured.");

        var normalizedLines = new List<ValidatedLine>(lines.Count);
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var merchant = Normalize(line.Merchant);
            var description = Normalize(line.Description);
            if (line.Id is <= 0)
                AddError(errors, $"lines[{index}].id", "The line identifier must be greater than zero.");
            if (line.ExpenseDate == DateOnly.MinValue)
                AddError(errors, $"lines[{index}].expenseDate", "Choose an expense date.");
            if (merchant.Length is 0 or > 160)
                AddError(errors, $"lines[{index}].merchant", "A merchant name of 1–160 characters is required.");
            if (description.Length is 0 or > 500)
                AddError(errors, $"lines[{index}].description", "A description of 1–500 characters is required.");
            if (line.Amount <= 0 || line.Amount > MaximumAmount)
                AddError(errors, $"lines[{index}].amount", "Enter an amount greater than zero within the supported range.");
            else if (decimal.Round(line.Amount, currency.DecimalPlaces) != line.Amount)
                AddError(errors, $"lines[{index}].amount", $"{currency.Code} supports {currency.DecimalPlaces} decimal places.");
            if (line.AccountId <= 0)
                AddError(errors, $"lines[{index}].accountId", "Choose an expense account.");
            normalizedLines.Add(new ValidatedLine(
                line.Id,
                line.ExpenseDate,
                merchant,
                description,
                line.AccountId,
                line.Amount));
        }

        if (normalizedLines.Sum(line => line.Amount) > MaximumAmount)
            AddError(errors, "lines", "The report total exceeds the supported amount range.");

        var accountIds = normalizedLines.Select(line => line.AccountId).Where(id => id > 0).Distinct().ToArray();
        var accountTypes = await (
            from account in db.ChartOfAccounts
            join accountGroup in db.AccountGroups
                on new { account.TenantId, account.CompanyId, AccountGroupId = account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where account.CompanyId == company.Id && accountIds.Contains(account.Id)
            select new { account.Id, account.IsActive, account.AllowPosting, accountGroup.AccountType })
            .ToDictionaryAsync(account => account.Id, cancellationToken);
        foreach (var line in normalizedLines.Select((value, index) => (value, index)))
        {
            if (!accountTypes.TryGetValue(line.value.AccountId, out var account) ||
                !account.IsActive ||
                !account.AllowPosting ||
                account.AccountType != "Expense")
            {
                AddError(errors, $"lines[{line.index}].accountId", "Choose an active, postable expense account in this company.");
            }
        }

        if (errors.Count != 0)
            throw new ExpenseReportValidationException(errors.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.ToArray(),
                StringComparer.Ordinal));
        return normalizedLines;
    }

    private async Task EnsureAccountTypeAsync(
        long companyId,
        long accountId,
        string accountType,
        CancellationToken cancellationToken)
    {
        var isValid = await (
            from account in db.ChartOfAccounts
            join accountGroup in db.AccountGroups
                on new { account.TenantId, account.CompanyId, AccountGroupId = account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where account.CompanyId == companyId &&
                  account.Id == accountId &&
                  account.IsActive &&
                  account.AllowPosting &&
                  accountGroup.AccountType == accountType
            select account.Id)
            .AnyAsync(cancellationToken);
        if (!isValid)
            throw Validation(accountType == "Liability" ? "employeePayableAccountId" : "cashAccountId",
                $"Choose an active, postable {accountType.ToLowerInvariant()} account in this company.");
    }

    private async Task EnsureExpenseLineAccountsAsync(
        long companyId,
        IEnumerable<ExpenseReportLine> lines,
        CancellationToken cancellationToken)
    {
        var accountIds = lines.Select(line => line.AccountId).Distinct().ToArray();
        var validAccountIds = await (
            from account in db.ChartOfAccounts
            join accountGroup in db.AccountGroups
                on new { account.TenantId, account.CompanyId, AccountGroupId = account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where account.CompanyId == companyId &&
                  accountIds.Contains(account.Id) &&
                  account.IsActive &&
                  account.AllowPosting &&
                  accountGroup.AccountType == "Expense"
            select account.Id)
            .ToListAsync(cancellationToken);
        if (validAccountIds.Count != accountIds.Length)
            throw Validation("lines", "Each line must use an active, postable expense account in this company.");
    }

    private async Task<ExpenseReport?> GetTrackedReportAsync(
        long companyId,
        long reportId,
        CancellationToken cancellationToken) =>
        await db.ExpenseReports
            .Include(report => report.Lines)
            .SingleOrDefaultAsync(report => report.CompanyId == companyId && report.Id == reportId, cancellationToken);

    private async Task<ExpenseReportDto> ToDtoAsync(
        ExpenseReport report,
        bool includeAll,
        long userId,
        CancellationToken cancellationToken) =>
        ToDto(
            report,
            await GetEmployeeNameAsync(report.EmployeeUserId, cancellationToken),
            includeAll,
            userId);

    private ExpenseReportDto ToDto(ExpenseReport report, string employeeName, bool includeAll, long userId)
    {
        var editable = report.EmployeeUserId == userId && report.Status is ("Draft" or "Rejected");
        var lines = report.Lines
            .OrderBy(line => line.Id)
            .Select(line => new ExpenseReportLineDto(
                line.Id,
                line.ExpenseDate,
                line.Merchant,
                line.Description,
                line.AccountId,
                line.Amount,
                line.ReceiptFileName is null
                    ? null
                    : $"/api/v1/companies/{report.CompanyId}/expense-reports/{report.Id}/lines/{line.Id}/receipt/{Uri.EscapeDataString(line.ReceiptFileName)}"))
            .ToArray();
        return new ExpenseReportDto(
            report.Id,
            report.CompanyId,
            report.EmployeeUserId,
            employeeName,
            report.ReportNumber,
            report.Title,
            report.CurrencyCode,
            report.TotalAmount,
            report.Status,
            report.ReviewNote,
            report.SubmittedAt,
            report.ReviewedAt,
            report.PostedAt,
            report.ReimbursedAt,
            report.JournalEntryId,
            report.ReimbursementJournalEntryId,
            editable,
            editable,
            includeAll && report.Status == "Submitted",
            includeAll && report.Status == "Approved",
            includeAll && report.Status == "Posted",
            lines);
    }

    private async Task<Dictionary<long, string>> GetEmployeeNamesAsync(
        IEnumerable<long> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToArray();
        return await db.Users
            .Where(user => ids.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email ?? "Employee" : user.DisplayName,
                cancellationToken);
    }

    private async Task<string> GetEmployeeNameAsync(long userId, CancellationToken cancellationToken) =>
        await db.Users
            .Where(user => user.Id == userId)
            .Select(user => string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email ?? "Employee" : user.DisplayName)
            .SingleOrDefaultAsync(cancellationToken) ?? "Employee";

    private static ExpenseReportLine ToEntity(long tenantId, long companyId, ValidatedLine line) =>
        new()
        {
            TenantId = tenantId,
            CompanyId = companyId,
            ExpenseDate = line.ExpenseDate,
            Merchant = line.Merchant,
            Description = line.Description,
            AccountId = line.AccountId,
            Amount = line.Amount
        };

    private static void EnsureEditable(ExpenseReport report, long userId)
    {
        if (report.EmployeeUserId != userId)
            throw new UnauthorizedAccessException("Only the employee who created this report can change it.");
        if (report.Status is not ("Draft" or "Rejected"))
            throw new ExpenseReportConflictException("Only draft or rejected expense reports can be changed.");
    }

    private long EnsureUser() =>
        currentTenant.UserId ?? throw new UnauthorizedAccessException("A user-scoped access token is required.");

    private async Task SaveWithConcurrencyHandlingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ExpenseReportConflictException("This expense report changed in another request. Reload it and try again.");
        }
    }

    private static string CreateReportNumber()
    {
        var token = Guid.NewGuid().ToString("N");
        return $"EXP-{DateTime.UtcNow:yyyyMMdd}-{token[..27]}".ToUpperInvariant();
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim();

    private static string? NormalizeNullable(string? value)
    {
        var normalized = Normalize(value);
        return normalized.Length == 0 ? null : normalized;
    }

    private static ExpenseReportValidationException Validation(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] });

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors.Add(key, messages);
        }

        messages.Add(message);
    }

    private sealed record ValidatedLine(
        long? Id,
        DateOnly ExpenseDate,
        string Merchant,
        string Description,
        long AccountId,
        decimal Amount);
}
