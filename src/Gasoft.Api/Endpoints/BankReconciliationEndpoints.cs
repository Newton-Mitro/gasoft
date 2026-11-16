using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Gasoft.Application.Abstractions;
using Gasoft.Application.Accounting;
using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Api.Endpoints;

public static class BankReconciliationEndpoints
{
    private const long MaximumStatementBytes = 10 * 1024 * 1024;
    private const int MaximumStatementRows = 10_000;

    public static IEndpointRouteBuilder MapBankReconciliationEndpoints(this IEndpointRouteBuilder app)
    {
        var accounts = app.MapGroup("/api/v1/companies/{companyId:long}/bank-accounts")
            .RequireAuthorization();

        accounts.MapGet("", async (
            long companyId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!await CompanyExistsAsync(db, companyId, cancellationToken))
                return Results.NotFound(new { error = "Company was not found in this workspace." });

            var rows = await (
                from bank in db.BankAccounts.AsNoTracking()
                join account in db.ChartOfAccounts.AsNoTracking() on bank.ChartOfAccountId equals account.Id
                where bank.CompanyId == companyId
                orderby bank.Name
                select new BankAccountResponse(
                    bank.Id,
                    bank.Name,
                    bank.BankName,
                    bank.AccountNumberLastFour,
                    bank.CurrencyCode,
                    bank.ChartOfAccountId,
                    account.Code,
                    account.Name,
                    bank.IsActive))
                .ToListAsync(cancellationToken);
            return Results.Ok(rows);
        })
        .WithName("GetBankAccounts");

        accounts.MapPost("", async (
            long companyId,
            BankAccountRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            if (!tenantId.HasValue || !await CompanyExistsAsync(db, companyId, cancellationToken))
                return Results.NotFound(new { error = "Company was not found in this workspace." });

            var name = request.Name?.Trim() ?? string.Empty;
            var bankName = request.BankName?.Trim() ?? string.Empty;
            var lastFour = request.AccountNumberLastFour?.Trim() ?? string.Empty;
            var errors = new Dictionary<string, string[]>();
            if (name.Length is 0 or > 120) errors["name"] = ["Account name is required and must be at most 120 characters."];
            if (bankName.Length is 0 or > 120) errors["bankName"] = ["Bank name is required and must be at most 120 characters."];
            if (lastFour.Length != 4 || !lastFour.All(char.IsAsciiLetterOrDigit))
                errors["accountNumberLastFour"] = ["Enter the last four letters or digits of the account number."];

            var companyCurrency = await db.Companies
                .Where(company => company.Id == companyId)
                .Select(company => company.FunctionalCurrencyCode)
                .SingleAsync(cancellationToken);
            if (request.CurrencyCode?.Trim().ToUpperInvariant() != companyCurrency)
                errors["currencyCode"] = ["The bank account currency must match the company's functional currency for reconciliation."];

            var account = await (
                from chart in db.ChartOfAccounts
                join accountGroup in db.AccountGroups on chart.AccountGroupId equals accountGroup.Id
                where chart.Id == request.ChartOfAccountId &&
                      chart.CompanyId == companyId &&
                      chart.IsActive &&
                      chart.AllowPosting &&
                      accountGroup.AccountType == "Asset"
                select chart)
                .SingleOrDefaultAsync(cancellationToken);
            if (account is null)
                errors["chartOfAccountId"] = ["Choose an active, postable asset account in this company."];
            if (errors.Count != 0) return Results.ValidationProblem(errors);

            var bankAccount = new BankAccount
            {
                TenantId = tenantId.Value,
                CompanyId = companyId,
                ChartOfAccountId = account!.Id,
                Name = name,
                BankName = bankName,
                AccountNumberLastFour = lastFour.ToUpperInvariant(),
                CurrencyCode = companyCurrency,
                IsActive = true
            };
            db.BankAccounts.Add(bankAccount);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                return Results.Conflict(new { error = "This chart-of-accounts entry is already linked to a bank account." });
            }

            return Results.Created(
                $"/api/v1/companies/{companyId}/bank-accounts/{bankAccount.Id}",
                new BankAccountResponse(
                    bankAccount.Id,
                    bankAccount.Name,
                    bankAccount.BankName,
                    bankAccount.AccountNumberLastFour,
                    bankAccount.CurrencyCode,
                    bankAccount.ChartOfAccountId,
                    account.Code,
                    account.Name,
                    bankAccount.IsActive));
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("CreateBankAccount");

        accounts.MapGet("/{bankAccountId:long}/transactions", async (
            long companyId,
            long bankAccountId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!await GetBankAccountAsync(db, companyId, bankAccountId).AnyAsync(cancellationToken))
                return Results.NotFound(new { error = "Bank account was not found in this company." });

            var transactions = await db.BankStatementTransactions.AsNoTracking()
                .Where(item => item.CompanyId == companyId && item.BankAccountId == bankAccountId)
                .OrderByDescending(item => item.TransactionDate)
                .ThenByDescending(item => item.Id)
                .Select(item => new BankTransactionResponse(
                    item.Id,
                    item.TransactionDate,
                    item.Description,
                    item.Reference,
                    item.Amount,
                    0,
                    item.Amount,
                    Array.Empty<BankMatchResponse>()))
                .ToListAsync(cancellationToken);
            var transactionIds = transactions.Select(item => item.Id).ToArray();
            var matchRows = await (
                from match in db.BankReconciliationMatches.AsNoTracking()
                join line in db.JournalLines.AsNoTracking()
                    on new { match.TenantId, match.CompanyId, JournalLineId = match.JournalLineId }
                    equals new { line.TenantId, line.CompanyId, JournalLineId = line.Id }
                join entry in db.JournalEntries.AsNoTracking()
                    on new { line.TenantId, line.CompanyId, line.JournalEntryId }
                    equals new { entry.TenantId, entry.CompanyId, JournalEntryId = entry.Id }
                where match.CompanyId == companyId &&
                      match.BankAccountId == bankAccountId &&
                      transactionIds.Contains(match.BankStatementTransactionId)
                select new
                {
                    match.BankStatementTransactionId,
                    Match = new BankMatchResponse(
                        match.Id,
                        match.JournalLineId,
                        match.Amount,
                        entry.EntryDate,
                        entry.Description)
                })
                .ToListAsync(cancellationToken);
            var matchesByTransaction = matchRows
                .GroupBy(item => item.BankStatementTransactionId)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Match).ToArray());
            return Results.Ok(transactions.Select(item =>
            {
                var matches = matchesByTransaction.GetValueOrDefault(item.Id, []);
                var matchedAmount = matches.Sum(match => match.Amount);
                return item with
                {
                    MatchedAmount = matchedAmount,
                    RemainingAmount = item.Amount - matchedAmount,
                    Matches = matches
                };
            }));
        })
        .WithName("GetBankStatementTransactions");

        accounts.MapPost("/{bankAccountId:long}/statement", async (
            long companyId,
            long bankAccountId,
            IFormFile file,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var bankAccount = await GetBankAccountAsync(db, companyId, bankAccountId)
                .SingleOrDefaultAsync(cancellationToken);
            if (bankAccount is null)
                return Results.NotFound(new { error = "Bank account was not found in this company." });
            if (!bankAccount.IsActive)
                return Results.Conflict(new { error = "Activate this bank account before importing a statement." });
            if (file.Length == 0 || file.Length > MaximumStatementBytes)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["Choose a non-empty CSV file no larger than 10 MB."]
                });

            await using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var csv = await reader.ReadToEndAsync(cancellationToken);
            var parsed = ParseStatement(csv);
            if (parsed.Errors.Count != 0) return Results.ValidationProblem(parsed.Errors);

            var existingKeys = await db.BankStatementTransactions
                .Where(item => item.CompanyId == companyId && item.BankAccountId == bankAccountId)
                .Select(item => item.ImportKey)
                .ToListAsync(cancellationToken);
            var existing = existingKeys.ToHashSet(StringComparer.Ordinal);
            var occurrence = new Dictionary<string, int>(StringComparer.Ordinal);
            var imported = new List<BankStatementTransaction>();
            var duplicateCount = 0;
            foreach (var row in parsed.Rows)
            {
                var canonical = $"{row.Date:yyyy-MM-dd}\n{row.Description}\n{row.Reference}\n{row.Amount.ToString(CultureInfo.InvariantCulture)}";
                var ordinal = occurrence.GetValueOrDefault(canonical);
                occurrence[canonical] = ordinal + 1;
                var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{canonical}\n{ordinal}")));
                if (!existing.Add(key))
                {
                    duplicateCount++;
                    continue;
                }

                imported.Add(new BankStatementTransaction
                {
                    TenantId = bankAccount.TenantId,
                    CompanyId = companyId,
                    BankAccountId = bankAccountId,
                    TransactionDate = row.Date,
                    Description = row.Description,
                    Reference = row.Reference,
                    Amount = row.Amount,
                    ImportKey = key
                });
            }

            db.BankStatementTransactions.AddRange(imported);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { imported = imported.Count, duplicates = duplicateCount, totalRows = parsed.Rows.Count });
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .DisableAntiforgery()
        .WithName("ImportBankStatement");

        accounts.MapGet("/{bankAccountId:long}/transactions/{transactionId:long}/matches", async (
            long companyId,
            long bankAccountId,
            long transactionId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var bankAccount = await GetBankAccountAsync(db, companyId, bankAccountId)
                .SingleOrDefaultAsync(cancellationToken);
            if (bankAccount is null)
                return Results.NotFound(new { error = "Bank account was not found in this company." });
            var transaction = await db.BankStatementTransactions.SingleOrDefaultAsync(
                item => item.Id == transactionId &&
                        item.CompanyId == companyId &&
                        item.BankAccountId == bankAccountId,
                cancellationToken);
            if (transaction is null)
                return Results.NotFound(new { error = "Statement transaction was not found." });

            var matchedAmount = await db.BankReconciliationMatches.AsNoTracking()
                .Where(match => match.BankStatementTransactionId == transactionId &&
                                match.CompanyId == companyId &&
                                match.BankAccountId == bankAccountId)
                .SumAsync(match => (decimal?)match.Amount, cancellationToken) ?? 0;
            var remainingAmount = transaction.Amount - matchedAmount;
            if (remainingAmount == 0) return Results.Ok(Array.Empty<BankMatchCandidateResponse>());

            var candidateRows = await (
                from line in db.JournalLines.AsNoTracking()
                join entry in db.JournalEntries.AsNoTracking()
                    on new { line.TenantId, line.CompanyId, line.JournalEntryId }
                    equals new { entry.TenantId, entry.CompanyId, JournalEntryId = entry.Id }
                let availableAmount = line.FunctionalDebit - line.FunctionalCredit -
                    (db.BankReconciliationMatches
                        .Where(match => match.CompanyId == companyId &&
                                        match.BankAccountId == bankAccountId &&
                                        match.JournalLineId == line.Id)
                        .Sum(match => (decimal?)match.Amount) ?? 0)
                where line.CompanyId == companyId &&
                      line.AccountId == bankAccount.ChartOfAccountId &&
                      entry.Status == "Posted" &&
                      availableAmount != 0 &&
                      availableAmount * remainingAmount > 0
                orderby entry.EntryDate descending
                select new { line.Id, entry.EntryDate, entry.Description, Amount = availableAmount })
                .Take(25)
                .ToListAsync(cancellationToken);

            return Results.Ok(candidateRows.Select(row => new BankMatchCandidateResponse(
                row.Id,
                row.EntryDate,
                row.Description,
                row.Amount)));
        })
        .WithName("GetBankTransactionMatches");

        accounts.MapPost("/{bankAccountId:long}/transactions/{transactionId:long}/match", async (
            long companyId,
            long bankAccountId,
            long transactionId,
            BankMatchRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            await using var transactionScope = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);
            var bankAccount = await GetBankAccountAsync(db, companyId, bankAccountId)
                .SingleOrDefaultAsync(cancellationToken);
            if (bankAccount is null)
                return Results.NotFound(new { error = "Bank account was not found in this company." });
            var transaction = await db.BankStatementTransactions.SingleOrDefaultAsync(
                item => item.Id == transactionId &&
                        item.CompanyId == companyId &&
                        item.BankAccountId == bankAccountId,
                cancellationToken);
            if (transaction is null)
                return Results.NotFound(new { error = "Statement transaction was not found." });
            var matchedAmount = await db.BankReconciliationMatches
                .Where(match => match.BankStatementTransactionId == transactionId &&
                                match.CompanyId == companyId &&
                                match.BankAccountId == bankAccountId)
                .SumAsync(match => (decimal?)match.Amount, cancellationToken) ?? 0;
            var remainingAmount = transaction.Amount - matchedAmount;
            var amount = request.Amount ?? remainingAmount;
            if (amount == 0 || amount * remainingAmount <= 0 ||
                Math.Abs(amount) > Math.Abs(remainingAmount))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["amount"] = ["Enter a non-zero amount in the statement direction that does not exceed its remaining balance."]
                });

            var line = await (
                from journalLine in db.JournalLines
                join entry in db.JournalEntries
                    on new { journalLine.TenantId, journalLine.CompanyId, journalLine.JournalEntryId }
                    equals new { entry.TenantId, entry.CompanyId, JournalEntryId = entry.Id }
                where journalLine.Id == request.JournalLineId &&
                      journalLine.CompanyId == companyId &&
                      journalLine.AccountId == bankAccount.ChartOfAccountId &&
                      entry.Status == "Posted"
                select journalLine)
                .SingleOrDefaultAsync(cancellationToken);
            if (line is null)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["journalLineId"] = ["Choose an unmatched posted entry on this bank account."]
                });
            var lineAmount = line.FunctionalDebit - line.FunctionalCredit;
            if (lineAmount * amount <= 0)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["journalLineId"] = ["Choose a posted ledger entry with the same debit or credit direction as the statement transaction."]
                });
            var allocatedLineAmount = await db.BankReconciliationMatches
                .Where(match => match.CompanyId == companyId &&
                                match.BankAccountId == bankAccountId &&
                                match.JournalLineId == line.Id)
                .SumAsync(match => (decimal?)match.Amount, cancellationToken) ?? 0;
            var availableLineAmount = lineAmount - allocatedLineAmount;
            if (availableLineAmount * amount <= 0 || Math.Abs(amount) > Math.Abs(availableLineAmount))
                return Results.Conflict(new { error = "The selected ledger entry does not have enough unmatched balance for this allocation." });

            var existingMatch = await db.BankReconciliationMatches.SingleOrDefaultAsync(
                match => match.CompanyId == companyId &&
                         match.BankAccountId == bankAccountId &&
                         match.BankStatementTransactionId == transactionId &&
                         match.JournalLineId == line.Id,
                cancellationToken);
            if (existingMatch is null)
            {
                db.BankReconciliationMatches.Add(new BankReconciliationMatch
                {
                    TenantId = bankAccount.TenantId,
                    CompanyId = companyId,
                    BankAccountId = bankAccountId,
                    BankStatementTransactionId = transactionId,
                    JournalLineId = line.Id,
                    Amount = amount
                });
            }
            else
            {
                existingMatch.Amount += amount;
            }
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transactionScope.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                return Results.Conflict(new { error = "The statement transaction or ledger entry was matched concurrently." });
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is PostgresException postgresException &&
                IsSerializationFailure(postgresException))
            {
                return Results.Conflict(new { error = "This reconciliation changed concurrently. Refresh the transaction and try again." });
            }
            catch (PostgresException exception) when (IsSerializationFailure(exception))
            {
                return Results.Conflict(new { error = "This reconciliation changed concurrently. Refresh the transaction and try again." });
            }

            return Results.Ok(new { transaction.Id, JournalLineId = line.Id, Amount = amount });
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("MatchBankTransaction");

        accounts.MapDelete("/{bankAccountId:long}/transactions/{transactionId:long}/matches/{matchId:long}", async (
            long companyId,
            long bankAccountId,
            long transactionId,
            long matchId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var match = await db.BankReconciliationMatches.SingleOrDefaultAsync(
                item => item.Id == matchId &&
                        item.CompanyId == companyId &&
                        item.BankAccountId == bankAccountId &&
                        item.BankStatementTransactionId == transactionId,
                cancellationToken);
            if (match is null) return Results.NotFound(new { error = "Reconciliation allocation was not found." });
            db.BankReconciliationMatches.Remove(match);
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("UnmatchBankReconciliationAllocation");

        accounts.MapDelete("/{bankAccountId:long}/transactions/{transactionId:long}/match", async (
            long companyId,
            long bankAccountId,
            long transactionId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var transaction = await db.BankStatementTransactions.SingleOrDefaultAsync(
                item => item.Id == transactionId &&
                        item.CompanyId == companyId &&
                        item.BankAccountId == bankAccountId,
                cancellationToken);
            if (transaction is null) return Results.NotFound(new { error = "Statement transaction was not found." });
            var matches = await db.BankReconciliationMatches.Where(
                    item => item.CompanyId == companyId &&
                            item.BankAccountId == bankAccountId &&
                            item.BankStatementTransactionId == transactionId)
                .ToListAsync(cancellationToken);
            if (matches.Count == 0) return Results.Conflict(new { error = "This transaction is not matched." });
            db.BankReconciliationMatches.RemoveRange(matches);
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("UnmatchBankTransaction");

        accounts.MapPost("/{bankAccountId:long}/transactions/{transactionId:long}/adjustment", async (
            long companyId,
            long bankAccountId,
            long transactionId,
            BankAdjustmentRequest request,
            AppDbContext db,
            IAccountingService accounting,
            CancellationToken cancellationToken) =>
        {
            await using var transactionScope = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);
            var bankAccount = await GetBankAccountAsync(db, companyId, bankAccountId)
                .SingleOrDefaultAsync(cancellationToken);
            if (bankAccount is null)
                return Results.NotFound(new { error = "Bank account was not found in this company." });
            var statement = await db.BankStatementTransactions.SingleOrDefaultAsync(
                item => item.Id == transactionId &&
                        item.CompanyId == companyId &&
                        item.BankAccountId == bankAccountId,
                cancellationToken);
            if (statement is null)
                return Results.NotFound(new { error = "Statement transaction was not found." });

            var alreadyAllocated = await db.BankReconciliationMatches
                .Where(match => match.CompanyId == companyId &&
                                match.BankAccountId == bankAccountId &&
                                match.BankStatementTransactionId == transactionId)
                .SumAsync(match => (decimal?)match.Amount, cancellationToken) ?? 0;
            var adjustmentAmount = statement.Amount - alreadyAllocated;
            if (adjustmentAmount == 0)
                return Results.Conflict(new { error = "This statement transaction has no remaining amount to adjust." });

            var offsetAccount = await db.ChartOfAccounts.SingleOrDefaultAsync(
                account => account.Id == request.OffsetAccountId &&
                           account.CompanyId == companyId &&
                           account.IsActive &&
                           account.AllowPosting,
                cancellationToken);
            if (offsetAccount is null || offsetAccount.Id == bankAccount.ChartOfAccountId)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["offsetAccountId"] = ["Choose an active, postable offset account other than this bank account."]
                });

            var entryDate = request.EntryDate ?? statement.TransactionDate;
            var period = await db.AccountingPeriods.SingleOrDefaultAsync(
                item => item.CompanyId == companyId &&
                        item.StartsOn <= entryDate &&
                        item.EndsOn >= entryDate,
                cancellationToken);
            if (period is null || period.Status != "Open")
                return Results.Conflict(new { error = "Choose an entry date in an open accounting period before recording this adjustment." });

            var journal = await db.Journals
                .Where(item => item.CompanyId == companyId && item.IsActive)
                .OrderByDescending(item => item.JournalType == "Bank")
                .ThenBy(item => item.Code)
                .FirstOrDefaultAsync(cancellationToken);
            if (journal is null)
                return Results.Conflict(new { error = "Create an active journal before recording a reconciliation adjustment." });

            var companyCurrency = await db.Companies
                .Where(item => item.Id == companyId)
                .Select(item => item.FunctionalCurrencyCode)
                .SingleAsync(cancellationToken);
            var absoluteAmount = Math.Abs(adjustmentAmount);
            var isDeposit = adjustmentAmount > 0;
            var description = string.IsNullOrWhiteSpace(request.Description)
                ? $"Bank reconciliation adjustment: {statement.Description}"
                : request.Description.Trim();
            if (description.Length > 500)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["description"] = ["Adjustment description must be at most 500 characters."]
                });

            try
            {
                var draft = await accounting.CreateDraftAsync(companyId, new CreateJournalEntryRequest(
                    journal.Id,
                    period.Id,
                    entryDate,
                    description,
                    [
                        new CreateJournalLineRequest(
                            bankAccount.ChartOfAccountId,
                            companyCurrency,
                            isDeposit ? absoluteAmount : 0,
                            isDeposit ? 0 : absoluteAmount,
                            1,
                            statement.Description),
                        new CreateJournalLineRequest(
                            offsetAccount.Id,
                            companyCurrency,
                            isDeposit ? 0 : absoluteAmount,
                            isDeposit ? absoluteAmount : 0,
                            1,
                            description)
                    ]), cancellationToken);
                if (draft is null)
                    return Results.NotFound(new { error = "Company was not found in this workspace." });

                var posted = await accounting.PostAsync(companyId, draft.Id, cancellationToken);
                if (posted is null)
                    return Results.NotFound(new { error = "The reconciliation adjustment journal entry was not found." });
                var bankLine = posted.Lines.Single(line => line.AccountId == bankAccount.ChartOfAccountId);
                db.BankReconciliationMatches.Add(new BankReconciliationMatch
                {
                    TenantId = bankAccount.TenantId,
                    CompanyId = companyId,
                    BankAccountId = bankAccountId,
                    BankStatementTransactionId = transactionId,
                    JournalLineId = bankLine.Id,
                    Amount = adjustmentAmount
                });
                await db.SaveChangesAsync(cancellationToken);
                await transactionScope.CommitAsync(cancellationToken);
                return Results.Created(
                    $"/api/v1/companies/{companyId}/journal-entries/{posted.Id}",
                    new BankAdjustmentResponse(posted.Id, bankLine.Id, adjustmentAmount, posted.Status));
            }
            catch (AccountingValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors.ToDictionary(error => error.Key, error => error.Value));
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is PostgresException postgresException &&
                (IsSerializationFailure(postgresException) || IsUniqueConstraintViolation(exception)))
            {
                return Results.Conflict(new { error = "This reconciliation changed concurrently. Refresh the transaction and try again." });
            }
            catch (PostgresException exception) when (IsSerializationFailure(exception))
            {
                return Results.Conflict(new { error = "This reconciliation changed concurrently. Refresh the transaction and try again." });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("CreateBankReconciliationAdjustment");

        return app;
    }

    private static IQueryable<BankAccount> GetBankAccountAsync(
        AppDbContext db,
        long companyId,
        long bankAccountId) =>
        db.BankAccounts.Where(item => item.CompanyId == companyId && item.Id == bankAccountId);

    private static Task<bool> CompanyExistsAsync(AppDbContext db, long companyId, CancellationToken cancellationToken) =>
        db.Companies.AnyAsync(item => item.Id == companyId, cancellationToken);

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static bool IsSerializationFailure(PostgresException exception) =>
        exception.SqlState == PostgresErrorCodes.SerializationFailure;

    private static ParsedStatement ParseStatement(string content)
    {
        var errors = new Dictionary<string, string[]>();
        var rows = new List<ParsedStatementRow>();
        var lines = content.Split('\n');
        if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
        {
            errors["file"] = ["The CSV file is empty."];
            return new ParsedStatement(rows, errors);
        }

        var header = ParseCsvLine(lines[0].TrimEnd('\r'));
        if (header.Errors.Count != 0)
        {
            errors["file"] = ["The CSV header is malformed."];
            return new ParsedStatement(rows, errors);
        }
        var indexes = header.Values
            .Select((value, index) => (Key: value.Trim().ToLowerInvariant(), Index: index))
            .ToDictionary(item => item.Key, item => item.Index, StringComparer.Ordinal);
        if (!indexes.TryGetValue("date", out var dateIndex) ||
            !indexes.TryGetValue("description", out var descriptionIndex) ||
            !indexes.TryGetValue("amount", out var amountIndex))
        {
            errors["file"] = ["CSV headers must include date, description, and amount columns. The optional reference column is supported."];
            return new ParsedStatement(rows, errors);
        }
        indexes.TryGetValue("reference", out var referenceIndex);
        var hasReference = indexes.ContainsKey("reference");

        var lineNumber = 1;
        foreach (var sourceLine in lines.Skip(1))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(sourceLine)) continue;
            if (rows.Count >= MaximumStatementRows)
            {
                errors["file"] = [$"A statement may contain no more than {MaximumStatementRows} rows."];
                return new ParsedStatement([], errors);
            }

            var parsed = ParseCsvLine(sourceLine.TrimEnd('\r'));
            if (parsed.Errors.Count != 0 || parsed.Values.Count < header.Values.Count)
            {
                errors[$"row{lineNumber}"] = ["The CSV row is malformed or has missing columns."];
                continue;
            }

            var dateValue = parsed.Values[dateIndex].Trim();
            var description = parsed.Values[descriptionIndex].Trim();
            var amountValue = parsed.Values[amountIndex].Trim();
            var reference = hasReference ? parsed.Values[referenceIndex].Trim() : string.Empty;
            if (!DateOnly.TryParseExact(dateValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                errors[$"row{lineNumber}"] = ["Date must use the ISO format YYYY-MM-DD."];
            else if (description.Length is 0 or > 500)
                errors[$"row{lineNumber}"] = ["Description is required and must be at most 500 characters."];
            else if (!decimal.TryParse(amountValue, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount) ||
                     amount == 0 || Math.Abs(amount) > 99_999_999_999_999.999999m ||
                     decimal.Round(amount, 6) != amount)
                errors[$"row{lineNumber}"] = ["Amount must be non-zero, use a decimal point, and have at most 6 decimal places."];
            else if (reference.Length > 120)
                errors[$"row{lineNumber}"] = ["Reference must be at most 120 characters."];
            else
                rows.Add(new ParsedStatementRow(date, description, reference, amount));
        }

        if (rows.Count == 0 && errors.Count == 0)
            errors["file"] = ["The CSV file does not contain any transactions."];
        return new ParsedStatement(rows, errors);
    }

    private static ParsedCsvLine ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == ',' && !quoted)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(character);
            }
        }

        if (quoted) return new ParsedCsvLine([], ["Unclosed quoted field."]);
        fields.Add(field.ToString());
        return new ParsedCsvLine(fields, []);
    }

    public sealed record BankAccountRequest(
        string? Name,
        string? BankName,
        string? AccountNumberLastFour,
        string? CurrencyCode,
        long ChartOfAccountId);
    public sealed record BankAccountResponse(
        long Id,
        string Name,
        string BankName,
        string AccountNumberLastFour,
        string CurrencyCode,
        long ChartOfAccountId,
        string ChartOfAccountCode,
        string ChartOfAccountName,
        bool IsActive);
    public sealed record BankTransactionResponse(
        long Id,
        DateOnly TransactionDate,
        string Description,
        string? Reference,
        decimal Amount,
        decimal MatchedAmount,
        decimal RemainingAmount,
        IReadOnlyList<BankMatchResponse> Matches);
    public sealed record BankMatchResponse(
        long Id,
        long JournalLineId,
        decimal Amount,
        DateOnly EntryDate,
        string Description);
    public sealed record BankMatchCandidateResponse(
        long JournalLineId,
        DateOnly EntryDate,
        string Description,
        decimal Amount);
    public sealed record BankMatchRequest(long JournalLineId, decimal? Amount = null);
    public sealed record BankAdjustmentRequest(long OffsetAccountId, DateOnly? EntryDate, string? Description);
    public sealed record BankAdjustmentResponse(long JournalEntryId, long JournalLineId, decimal Amount, string Status);
    private sealed record ParsedStatementRow(DateOnly Date, string Description, string Reference, decimal Amount);
    private sealed record ParsedStatement(List<ParsedStatementRow> Rows, Dictionary<string, string[]> Errors);
    private sealed record ParsedCsvLine(List<string> Values, List<string> Errors);
}
