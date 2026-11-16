using Gasoft.Application.Abstractions;
using Gasoft.Application.Consolidation;

namespace Gasoft.Api.Endpoints;

public static class ConsolidationEndpoints
{
    public static IEndpointRouteBuilder MapConsolidationEndpoints(this IEndpointRouteBuilder app)
    {
        var groups = app.MapGroup("/api/v1/tenant/consolidation-groups").RequireAuthorization();

        groups.MapGet("", async (
            IConsolidationService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.GetGroupsAsync(cancellationToken)))
            .WithName("GetConsolidationGroups");

        groups.MapGet("/companies", async (
            IConsolidationService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.GetCompaniesAsync(cancellationToken)))
            .WithName("GetConsolidationCompanies");

        groups.MapPost("", async (
            ConsolidationGroupRequest request,
            IConsolidationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var group = await service.CreateGroupAsync(request, cancellationToken);
                return group is null
                    ? Results.Unauthorized()
                    : Results.Created($"/api/v1/tenant/consolidation-groups/{group.Id}", group);
            }
            catch (ConsolidationValidationException exception) { return ValidationProblem(exception); }
            catch (ConsolidationConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("CreateConsolidationGroup");

        groups.MapPut("/{groupId:long}", async (
            long groupId,
            ConsolidationGroupRequest request,
            IConsolidationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var group = await service.UpdateGroupAsync(groupId, request, cancellationToken);
                return group is null
                    ? Results.NotFound(new { error = "Consolidation group was not found." })
                    : Results.Ok(group);
            }
            catch (ConsolidationValidationException exception) { return ValidationProblem(exception); }
            catch (ConsolidationConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("UpdateConsolidationGroup");

        groups.MapGet("/{groupId:long}/runs", async (
            long groupId,
            IConsolidationService service,
            CancellationToken cancellationToken) =>
        {
            var runs = await service.GetRunsAsync(groupId, cancellationToken);
            return runs is null
                ? Results.NotFound(new { error = "Consolidation group was not found." })
                : Results.Ok(runs);
        })
        .WithName("GetConsolidationRuns");

        groups.MapPost("/{groupId:long}/runs", async (
            long groupId,
            CreateConsolidationRunRequest request,
            IConsolidationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var run = await service.CreateRunAsync(groupId, request, cancellationToken);
                return run is null
                    ? Results.NotFound(new { error = "Consolidation group was not found." })
                    : Results.Created(
                        $"/api/v1/tenant/consolidation-groups/{groupId}/runs/{run.Id}",
                        run);
            }
            catch (ConsolidationValidationException exception) { return ValidationProblem(exception); }
            catch (ConsolidationConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("CreateConsolidationRun");

        groups.MapGet("/{groupId:long}/runs/{runId:long}", async (
            long groupId,
            long runId,
            IConsolidationService service,
            CancellationToken cancellationToken) =>
        {
            var run = await service.GetRunAsync(groupId, runId, cancellationToken);
            return run is null
                ? Results.NotFound(new { error = "Consolidation run was not found." })
                : Results.Ok(run);
        })
        .WithName("GetConsolidationRun");

        groups.MapPut("/{groupId:long}/runs/{runId:long}/eliminations", async (
            long groupId,
            long runId,
            SaveEliminationsRequest request,
            IConsolidationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return await service.SaveEliminationsAsync(groupId, runId, request, cancellationToken)
                    ? Results.NoContent()
                    : Results.NotFound(new { error = "Consolidation run was not found." });
            }
            catch (ConsolidationValidationException exception) { return ValidationProblem(exception); }
            catch (ConsolidationConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("SaveConsolidationEliminations");

        groups.MapPost("/{groupId:long}/runs/{runId:long}/finalize", async (
            long groupId,
            long runId,
            IConsolidationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var run = await service.FinalizeRunAsync(groupId, runId, cancellationToken);
                return run is null
                    ? Results.NotFound(new { error = "Consolidation run was not found." })
                    : Results.Ok(run);
            }
            catch (ConsolidationConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("FinalizeConsolidationRun");

        return app;
    }

    private static IResult ValidationProblem(ConsolidationValidationException exception) =>
        Results.ValidationProblem(exception.Errors);
}
