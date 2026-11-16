using System.Text;
using Gasoft.Application.Abstractions;
using Gasoft.Application.BankReconciliation;

namespace Gasoft.Api.Endpoints;

public static class BankReconciliationEndpoints
{
    private const long MaximumStatementBytes = 10 * 1024 * 1024;

    public static IEndpointRouteBuilder MapBankReconciliationEndpoints(this IEndpointRouteBuilder app)
    {
        var accounts = app.MapGroup("/api/v1/companies/{companyId:long}/bank-accounts")
            .RequireAuthorization();

        accounts.MapGet("", async (
            long companyId,
            IBankReconciliationService service,
            CancellationToken cancellationToken) =>
        {
            var bankAccounts = await service.GetBankAccountsAsync(companyId, cancellationToken);
            return bankAccounts is null
                ? Results.NotFound(new { error = "Company was not found in this workspace." })
                : Results.Ok(bankAccounts);
        })
        .WithName("GetBankAccounts");

        accounts.MapPost("", async (
            long companyId,
            BankAccountRequest request,
            IBankReconciliationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var bankAccount = await service.CreateBankAccountAsync(companyId, request, cancellationToken);
                return bankAccount is null
                    ? Results.NotFound(new { error = "Company was not found in this workspace." })
                    : Results.Created(
                        $"/api/v1/companies/{companyId}/bank-accounts/{bankAccount.Id}",
                        bankAccount);
            }
            catch (BankReconciliationValidationException exception) { return ValidationProblem(exception); }
            catch (BankReconciliationConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("CreateBankAccount");

        accounts.MapGet("/{bankAccountId:long}/transactions", async (
            long companyId,
            long bankAccountId,
            IBankReconciliationService service,
            CancellationToken cancellationToken) =>
        {
            var transactions = await service.GetTransactionsAsync(companyId, bankAccountId, cancellationToken);
            return transactions is null
                ? Results.NotFound(new { error = "Bank account was not found in this company." })
                : Results.Ok(transactions);
        })
        .WithName("GetBankStatementTransactions");

        accounts.MapGet("/{bankAccountId:long}/transactions/page", async (
            long companyId,
            long bankAccountId,
            int page,
            int pageSize,
            IBankReconciliationService service,
            CancellationToken cancellationToken) =>
        {
            var transactions = await service.GetTransactionsPageAsync(
                companyId, bankAccountId, page, pageSize, cancellationToken);
            return transactions is null
                ? Results.NotFound(new { error = "Bank account was not found in this company." })
                : Results.Ok(transactions);
        })
        .WithName("GetBankStatementTransactionsPage");

        accounts.MapPost("/{bankAccountId:long}/statement", async (
            long companyId,
            long bankAccountId,
            IFormFile file,
            IBankReconciliationService service,
            CancellationToken cancellationToken) =>
        {
            if (file.Length == 0 || file.Length > MaximumStatementBytes)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["Choose a non-empty CSV file no larger than 10 MB."]
                });
            try
            {
                await using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var csv = await reader.ReadToEndAsync(cancellationToken);
                var result = await service.ImportStatementAsync(
                    companyId, bankAccountId, csv, cancellationToken);
                return Results.Ok(result);
            }
            catch (BankReconciliationNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
            catch (BankReconciliationValidationException exception) { return ValidationProblem(exception); }
            catch (BankReconciliationConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .DisableAntiforgery()
        .WithName("ImportBankStatement");

        accounts.MapGet("/{bankAccountId:long}/transactions/{transactionId:long}/matches", async (
            long companyId,
            long bankAccountId,
            long transactionId,
            IBankReconciliationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetMatchCandidatesAsync(
                    companyId, bankAccountId, transactionId, cancellationToken));
            }
            catch (BankReconciliationNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
        })
        .WithName("GetBankTransactionMatches");

        accounts.MapPost("/{bankAccountId:long}/transactions/{transactionId:long}/match", async (
            long companyId,
            long bankAccountId,
            long transactionId,
            BankMatchRequest request,
            IBankReconciliationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.MatchAsync(
                    companyId, bankAccountId, transactionId, request, cancellationToken);
                return Results.Ok(result);
            }
            catch (BankReconciliationNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
            catch (BankReconciliationValidationException exception) { return ValidationProblem(exception); }
            catch (BankReconciliationConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (BankReconciliationConcurrentException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("MatchBankTransaction");

        accounts.MapDelete("/{bankAccountId:long}/transactions/{transactionId:long}/matches/{matchId:long}", async (
            long companyId,
            long bankAccountId,
            long transactionId,
            long matchId,
            IBankReconciliationService service,
            CancellationToken cancellationToken) =>
        {
            var removed = await service.UnmatchAllocationAsync(
                companyId, bankAccountId, transactionId, matchId, cancellationToken);
            return removed
                ? Results.NoContent()
                : Results.NotFound(new { error = "Reconciliation allocation was not found." });
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("UnmatchBankReconciliationAllocation");

        accounts.MapDelete("/{bankAccountId:long}/transactions/{transactionId:long}/match", async (
            long companyId,
            long bankAccountId,
            long transactionId,
            IBankReconciliationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await service.UnmatchTransactionAsync(
                    companyId, bankAccountId, transactionId, cancellationToken);
                return Results.NoContent();
            }
            catch (BankReconciliationNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
            catch (BankReconciliationConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("UnmatchBankTransaction");

        accounts.MapPost("/{bankAccountId:long}/transactions/{transactionId:long}/adjustment", async (
            long companyId,
            long bankAccountId,
            long transactionId,
            BankAdjustmentRequest request,
            IBankReconciliationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.CreateAdjustmentAsync(
                    companyId, bankAccountId, transactionId, request, cancellationToken);
                return Results.Created(
                    $"/api/v1/companies/{companyId}/journal-entries/{result.JournalEntryId}",
                    result);
            }
            catch (BankReconciliationNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
            catch (BankReconciliationValidationException exception) { return ValidationProblem(exception); }
            catch (BankReconciliationConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (BankReconciliationConcurrentException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("CreateBankReconciliationAdjustment");

        return app;
    }

    private static IResult ValidationProblem(BankReconciliationValidationException exception) =>
        Results.ValidationProblem(exception.Errors);
}
