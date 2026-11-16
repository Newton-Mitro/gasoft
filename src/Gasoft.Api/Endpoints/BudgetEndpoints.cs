using Gasoft.Application.Abstractions;
using Gasoft.Application.Budgets;

namespace Gasoft.Api.Endpoints;

public static class BudgetEndpoints
{
    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var companies = app.MapGroup("/api/v1/companies/{companyId:long}/budgets").RequireAuthorization();

        companies.MapGet("", async (
            long companyId,
            long? fiscalYearId,
            IBudgetService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var workspace = await service.GetWorkspaceAsync(companyId, fiscalYearId, cancellationToken);
                return workspace is null
                    ? Results.NotFound(new { error = "Company was not found in this workspace." })
                    : Results.Ok(workspace);
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        })
        .WithName("GetBudgets");

        companies.MapPost("", async (
            long companyId,
            CreateBudgetRequest request,
            IBudgetService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var budget = await service.CreateAsync(companyId, request, cancellationToken);
                return budget is null
                    ? Results.NotFound(new { error = "Company was not found in this workspace." })
                    : Results.Created($"/api/v1/companies/{companyId}/budgets/{budget.Id}", budget);
            }
            catch (BudgetValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
            catch (BudgetConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("CreateBudget");

        companies.MapPut("/{budgetId:long}/lines", async (
            long companyId,
            long budgetId,
            SaveBudgetLinesRequest request,
            IBudgetService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await service.SaveLinesAsync(companyId, budgetId, request.Lines, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
            catch (BudgetValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("SaveBudgetLines");

        companies.MapDelete("/{budgetId:long}", async (
            long companyId,
            long budgetId,
            IBudgetService service,
            CancellationToken cancellationToken) =>
        {
            var deleted = await service.DeleteAsync(companyId, budgetId, cancellationToken);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = "Budget was not found for this company." });
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("DeleteBudget");

        var reports = app.MapGroup("/api/v1/companies/{companyId:long}/reports").RequireAuthorization();
        reports.MapGet("/budget-vs-actual", async (
            long companyId,
            long budgetId,
            IBudgetService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetBudgetVsActualAsync(companyId, budgetId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        })
        .WithName("GetBudgetVsActual");

        return app;
    }

    public sealed record SaveBudgetLinesRequest(IReadOnlyList<BudgetLineRequest>? Lines);
}
