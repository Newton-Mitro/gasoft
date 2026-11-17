using Gasoft.Application.Abstractions;
using Gasoft.Application.Reports;

namespace Gasoft.Api.Endpoints;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var companies = app.MapGroup("/api/v1/companies/{companyId:long}/reports")
            .RequireAuthorization()
            .AddEndpointFilterFactory(RequestShapeValidationEndpointFilter.Create);

        companies.MapGet("/cash-flow/accounts", async (
            long companyId,
            IReportingService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.GetCashFlowAccountsAsync(companyId, cancellationToken)));

        companies.MapPut("/cash-flow/accounts/{accountId:long}", async (
            long companyId,
            long accountId,
            CashFlowCategoryRequest request,
            IReportingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.UpdateCashFlowCategoryAsync(
                    companyId, accountId, request, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ReportValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
        });

        companies.MapGet("/cash-flow", async (
            long companyId,
            DateOnly? fromDate,
            DateOnly? toDate,
            IReportingService service,
            CancellationToken cancellationToken) =>
        {
            if (!fromDate.HasValue || !toDate.HasValue || fromDate > toDate)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["dateRange"] = ["Provide a start and end date; the end date must be on or after the start date."]
                });

            var report = await service.GetCashFlowAsync(
                companyId, fromDate.Value, toDate.Value, cancellationToken);
            return report is null
                ? Results.NotFound(new { error = "Company was not found in this tenant." })
                : Results.Ok(report);
        })
        .WithName("GetCashFlow");

        companies.MapGet("/retained-earnings", async (
            long companyId,
            long fiscalYearId,
            IReportingService service,
            CancellationToken cancellationToken) =>
        {
            var report = await service.GetRetainedEarningsRollForwardAsync(companyId, fiscalYearId, cancellationToken);
            return report is null
                ? Results.NotFound(new { error = "Company or fiscal year was not found in this tenant." })
                : Results.Ok(report);
        })
        .WithName("GetRetainedEarningsRollForward");

        companies.MapGet("/retained-earnings-roll-forward", async (
            long companyId,
            long fiscalYearId,
            IReportingService service,
            CancellationToken cancellationToken) =>
        {
            var report = await service.GetRetainedEarningsRollForwardAsync(companyId, fiscalYearId, cancellationToken);
            return report is null
                ? Results.NotFound(new { error = "Company or fiscal year was not found in this tenant." })
                : Results.Ok(report);
        })
        .WithName("GetRetainedEarningsRollForwardAlias");

        companies.MapGet("/trial-balance", async (
            long companyId,
            DateOnly? fromDate,
            DateOnly? toDate,
            IReportingService service,
            CancellationToken cancellationToken) =>
        {
            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["to"] = ["The end date must be on or after the start date."]
                });

            var report = await service.GetTrialBalanceAsync(companyId, fromDate, toDate, cancellationToken);
            return report is null
                ? Results.NotFound(new { error = "Company was not found in this tenant." })
                : Results.Ok(report);
        });

        companies.MapGet("/income-statement", async (
            long companyId,
            DateOnly? fromDate,
            DateOnly? toDate,
            IReportingService service,
            CancellationToken cancellationToken) =>
        {
            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["to"] = ["The end date must be on or after the start date."]
                });

            var report = await service.GetIncomeStatementAsync(companyId, fromDate, toDate, cancellationToken);
            return report is null
                ? Results.NotFound(new { error = "Company was not found in this tenant." })
                : Results.Ok(report);
        });

        companies.MapGet("/balance-sheet", async (
            long companyId,
            DateOnly? asOfDate,
            IReportingService service,
            CancellationToken cancellationToken) =>
        {
            var effectiveDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var report = await service.GetBalanceSheetAsync(companyId, effectiveDate, cancellationToken);
            return report is null
                ? Results.NotFound(new { error = "Company was not found in this tenant." })
                : Results.Ok(report);
        });

        companies.MapGet("/receivables-aging", async (
            long companyId,
            DateOnly? asOfDate,
            IReportingService service,
            CancellationToken cancellationToken) =>
        {
            var effectiveDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var report = await service.GetReceivablesAgingAsync(companyId, effectiveDate, cancellationToken);
            return report is null
                ? Results.NotFound(new { error = "Company was not found in this tenant." })
                : Results.Ok(report);
        })
        .WithName("GetReceivablesAging");

        companies.MapGet("/payables-aging", async (
            long companyId,
            DateOnly? asOfDate,
            IReportingService service,
            CancellationToken cancellationToken) =>
        {
            var effectiveDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var report = await service.GetPayablesAgingAsync(companyId, effectiveDate, cancellationToken);
            return report is null
                ? Results.NotFound(new { error = "Company was not found in this tenant." })
                : Results.Ok(report);
        })
        .WithName("GetPayablesAging");

        return app;
    }
}
