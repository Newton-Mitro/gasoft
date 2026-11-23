using Gasoft.Application.Abstractions;
using Gasoft.Application.Accounting;

namespace Gasoft.Api.Endpoints;

public static class AccountingEndpoints
{
    public static IEndpointRouteBuilder MapAccountingEndpoints(this IEndpointRouteBuilder app)
    {
        var companies = app.MapGroup("/api/v1/companies/{companyId:long}")
            .RequireAuthorization()
            .AddEndpointFilterFactory(RequestShapeValidationEndpointFilter.Create);

        companies.MapGet("/account-groups", async (
            long companyId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetAccountGroupsAsync(companyId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapGet("/account-groups/page", async (
            long companyId,
            IAccountingService service,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 25,
            string? search = null) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
            {
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            }

            try
            {
                return Results.Ok(await service.GetAccountGroupsPageAsync(
                    companyId, page, pageSize, search, cancellationToken));
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapGet("/account-groups/{groupId:long}", async (
            long companyId,
            long groupId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var group = await service.GetAccountGroupAsync(companyId, groupId, cancellationToken);
                return group is null
                    ? Results.NotFound(new { error = "Account group was not found in this company." })
                    : Results.Ok(group);
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPost("/account-groups", async (
            long companyId,
            CreateAccountGroupRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var group = await service.CreateAccountGroupAsync(companyId, request, cancellationToken);
                return Results.Created($"/api/v1/companies/{companyId}/account-groups/{group.Id}", group);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPut("/account-groups/{groupId:long}", async (
            long companyId,
            long groupId,
            UpdateAccountGroupRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var group = await service.UpdateAccountGroupAsync(
                    companyId, groupId, request, cancellationToken);
                return group is null
                    ? Results.NotFound(new { error = "Account group was not found in this company." })
                    : Results.Ok(group);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapGet("/accounts", async (
            long companyId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetAccountsAsync(companyId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapGet("/accounts/page", async (
            long companyId,
            IAccountingService service,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 25,
            long? accountGroupId = null,
            string? search = null) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
            {
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            }

            try
            {
                return Results.Ok(await service.GetAccountsPageAsync(
                    companyId, page, pageSize, accountGroupId, search, cancellationToken));
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapGet("/accounts/{accountId:long}", async (
            long companyId,
            long accountId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var account = await service.GetAccountAsync(companyId, accountId, cancellationToken);
                return account is null
                    ? Results.NotFound(new { error = "Account was not found in this company." })
                    : Results.Ok(account);
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapGet("/fiscal-years", async (
            long companyId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetFiscalYearsAsync(companyId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/fiscal-years/page", async (
            long companyId,
            IAccountingService service,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 25) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
            {
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            }

            try
            {
                return Results.Ok(await service.GetFiscalYearsPageAsync(companyId, page, pageSize, cancellationToken));
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/fiscal-years/{fiscalYearId:long}", async (
            long companyId,
            long fiscalYearId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            var year = await service.GetFiscalYearAsync(companyId, fiscalYearId, cancellationToken);
            return year is null ? Results.NotFound(new { error = "Fiscal year was not found in this company." }) : Results.Ok(year);
        });
        companies.MapGet("/fiscal-years/{fiscalYearId:long}/close-checklist", async (
            long companyId,
            long fiscalYearId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            var checklist = await service.GetFiscalYearCloseChecklistAsync(
                companyId, fiscalYearId, cancellationToken);
            return checklist is null
                ? Results.NotFound(new { error = "Fiscal year was not found in this company." })
                : Results.Ok(checklist);
        });
        companies.MapGet("/fiscal-years/{fiscalYearId:long}/status-history", async (
            long companyId,
            long fiscalYearId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            var history = await service.GetFiscalYearStatusHistoryAsync(
                companyId, fiscalYearId, cancellationToken);
            return history is null
                ? Results.NotFound(new { error = "Fiscal year was not found in this company." })
                : Results.Ok(history);
        });
        companies.MapPost("/fiscal-years/{fiscalYearId:long}/closing-journal", async (
            long companyId,
            long fiscalYearId,
            PrepareFiscalYearClosingRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var entry = await service.PrepareFiscalYearClosingJournalAsync(
                    companyId, fiscalYearId, request, cancellationToken);
                return entry is null
                    ? Results.NotFound(new { error = "Fiscal year was not found in this company." })
                    : Results.Created(
                        $"/api/v1/companies/{companyId}/journal-entries/{entry.Id}",
                        entry);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"));
        companies.MapPost("/fiscal-years/{fiscalYearId:long}/opening-balances", async (
            long companyId,
            long fiscalYearId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var entry = await service.PrepareFiscalYearOpeningBalancesAsync(companyId, fiscalYearId, cancellationToken);
                return entry is null
                    ? Results.NotFound(new { error = "Fiscal year was not found in this company." })
                    : Results.Created(
                        $"/api/v1/companies/{companyId}/journal-entries/{entry.Id}",
                        entry);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"));
        companies.MapPost("/fiscal-years/{fiscalYearId:long}/close", async (
            long companyId,
            long fiscalYearId,
            ChangeAccountingPeriodStatusRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var year = await service.CloseFiscalYearAsync(companyId, fiscalYearId, request, cancellationToken);
                return year is null
                    ? Results.NotFound(new { error = "Fiscal year was not found in this company." })
                    : Results.Ok(year);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"));
        companies.MapPost("/fiscal-years/{fiscalYearId:long}/reopen", async (
            long companyId,
            long fiscalYearId,
            ChangeAccountingPeriodStatusRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var year = await service.ReopenFiscalYearAsync(companyId, fiscalYearId, request, cancellationToken);
                return year is null
                    ? Results.NotFound(new { error = "Fiscal year was not found in this company." })
                    : Results.Ok(year);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"));

        companies.MapGet("/accounting-periods", async (
            long companyId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetPeriodsAsync(companyId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/accounting-periods/page", async (
            long companyId,
            IAccountingService service,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 25) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
            {
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            }

            try
            {
                return Results.Ok(await service.GetPeriodsPageAsync(companyId, page, pageSize, cancellationToken));
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/accounting-periods/{periodId:long}", async (
            long companyId,
            long periodId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            var period = await service.GetPeriodAsync(companyId, periodId, cancellationToken);
            return period is null ? Results.NotFound(new { error = "Accounting period was not found in this company." }) : Results.Ok(period);
        });
        companies.MapGet("/accounting-periods/{periodId:long}/status-history", async (
            long companyId,
            long periodId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            var history = await service.GetPeriodStatusHistoryAsync(companyId, periodId, cancellationToken);
            return history is null
                ? Results.NotFound(new { error = "Accounting period was not found in this company." })
                : Results.Ok(history);
        });
        companies.MapGet("/accounting-periods/{periodId:long}/close-checklist", async (
            long companyId,
            long periodId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            var checklist = await service.GetPeriodCloseChecklistAsync(companyId, periodId, cancellationToken);
            return checklist is null
                ? Results.NotFound(new { error = "Accounting period was not found in this company." })
                : Results.Ok(checklist);
        });
        companies.MapPost("/accounting-periods/{periodId:long}/close", async (
            long companyId,
            long periodId,
            ChangeAccountingPeriodStatusRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var period = await service.ClosePeriodAsync(companyId, periodId, request, cancellationToken);
                return period is null
                    ? Results.NotFound(new { error = "Accounting period was not found in this company." })
                    : Results.Ok(period);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"));
        companies.MapPost("/accounting-periods/{periodId:long}/reopen", async (
            long companyId,
            long periodId,
            ChangeAccountingPeriodStatusRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var period = await service.ReopenPeriodAsync(companyId, periodId, request, cancellationToken);
                return period is null
                    ? Results.NotFound(new { error = "Accounting period was not found in this company." })
                    : Results.Ok(period);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"));

        companies.MapGet("/journals", async (
            long companyId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetJournalsAsync(companyId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/journals/page", async (
            long companyId,
            IAccountingService service,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 25) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
            {
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            }

            try
            {
                return Results.Ok(await service.GetJournalsPageAsync(companyId, page, pageSize, cancellationToken));
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/journals/{journalId:long}", async (
            long companyId,
            long journalId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            var journal = await service.GetJournalAsync(companyId, journalId, cancellationToken);
            return journal is null ? Results.NotFound(new { error = "Journal was not found in this company." }) : Results.Ok(journal);
        });

        companies.MapPost("/accounts", async (
            long companyId,
            CreateAccountRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var account = await service.CreateAccountAsync(companyId, request, cancellationToken);
                return Results.Created($"/api/v1/companies/{companyId}/accounts/{account.Id}", account);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPut("/accounts/{accountId:long}", async (
            long companyId,
            long accountId,
            UpdateAccountRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var account = await service.UpdateAccountAsync(
                    companyId, accountId, request, cancellationToken);
                return account is null
                    ? Results.NotFound(new { error = "Account was not found in this company." })
                    : Results.Ok(account);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPost("/fiscal-years", async (
            long companyId,
            CreateFiscalYearRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var year = await service.CreateFiscalYearAsync(companyId, request, cancellationToken);
                return Results.Created($"/api/v1/companies/{companyId}/fiscal-years/{year.Id}", year);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapPut("/fiscal-years/{fiscalYearId:long}", async (
            long companyId,
            long fiscalYearId,
            UpdateFiscalYearRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var year = await service.UpdateFiscalYearAsync(companyId, fiscalYearId, request, cancellationToken);
                return year is null ? Results.NotFound(new { error = "Fiscal year was not found in this company." }) : Results.Ok(year);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPost("/accounting-periods", async (
            long companyId,
            CreateAccountingPeriodRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var period = await service.CreatePeriodAsync(companyId, request, cancellationToken);
                return Results.Created($"/api/v1/companies/{companyId}/accounting-periods/{period.Id}", period);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapPut("/accounting-periods/{periodId:long}", async (
            long companyId,
            long periodId,
            UpdateAccountingPeriodRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var period = await service.UpdatePeriodAsync(companyId, periodId, request, cancellationToken);
                return period is null ? Results.NotFound(new { error = "Accounting period was not found in this company." }) : Results.Ok(period);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPost("/journals", async (
            long companyId,
            CreateJournalRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var journal = await service.CreateJournalAsync(companyId, request, cancellationToken);
                return Results.Created($"/api/v1/companies/{companyId}/journals/{journal.Id}", journal);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapPut("/journals/{journalId:long}", async (
            long companyId,
            long journalId,
            UpdateJournalRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var journal = await service.UpdateJournalAsync(companyId, journalId, request, cancellationToken);
                return journal is null ? Results.NotFound(new { error = "Journal was not found in this company." }) : Results.Ok(journal);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPost("/journal-entries", async (
            long companyId,
            CreateJournalEntryRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var entry = await service.CreateDraftAsync(companyId, request, cancellationToken);
                return entry is null
                    ? Results.NotFound(new { error = "Company was not found in this tenant." })
                    : Results.Created(
                        $"/api/v1/companies/{companyId}/journal-entries/{entry.Id}",
                        entry);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        });

        companies.MapGet("/journal-entries", async (
            long companyId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetJournalEntriesAsync(companyId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/journal-entries/page", async (
            long companyId,
            IAccountingService service,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 25) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
            {
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            }

            try
            {
                return Results.Ok(await service.GetJournalEntriesPageAsync(companyId, page, pageSize, cancellationToken));
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapGet("/journal-entries/{entryId:long}", async (
            long companyId,
            long entryId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            var entry = await service.GetJournalEntryAsync(companyId, entryId, cancellationToken);
            return entry is null ? Results.NotFound() : Results.Ok(entry);
        });

        companies.MapPut("/journal-entries/{entryId:long}", async (
            long companyId,
            long entryId,
            CreateJournalEntryRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var entry = await service.UpdateDraftAsync(companyId, entryId, request, cancellationToken);
                return entry is null ? Results.NotFound() : Results.Ok(entry);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPost("/journal-entries/{entryId:long}/reverse", async (
            long companyId,
            long entryId,
            ReverseJournalEntryRequest request,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var entry = await service.ReverseJournalEntryAsync(
                    companyId, entryId, request, cancellationToken);
                return entry is null ? Results.NotFound() : Results.Created(
                    $"/api/v1/companies/{companyId}/journal-entries/{entry.Id}",
                    entry);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPost("/journal-entries/{entryId:long}/post", async (
            long companyId,
            long entryId,
            IAccountingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var entry = await service.PostAsync(companyId, entryId, cancellationToken);
                return entry is null ? Results.NotFound() : Results.Ok(entry);
            }
            catch (AccountingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (AccountingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        });

        return app;
    }

    private static IResult ValidationProblem(AccountingValidationException exception) =>
        Results.ValidationProblem(exception.Errors.ToDictionary(error => error.Key, error => error.Value));
}
