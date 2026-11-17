using Gasoft.Application.Abstractions;
using Gasoft.Application.FixedAssets;

namespace Gasoft.Api.Endpoints;

public static class FixedAssetEndpoints
{
    public static IEndpointRouteBuilder MapFixedAssetEndpoints(this IEndpointRouteBuilder app)
    {
        var assets = app.MapGroup("/api/v1/companies/{companyId:long}/fixed-assets")
            .RequireAuthorization()
            .AddEndpointFilterFactory(RequestShapeValidationEndpointFilter.Create);

        assets.MapGet("", async (
            long companyId,
            IFixedAssetService service,
            CancellationToken cancellationToken) =>
        {
            var workspace = await service.GetWorkspaceAsync(companyId, cancellationToken);
            return workspace is null
                ? Results.NotFound(new { error = "Company was not found in this workspace." })
                : Results.Ok(workspace);
        })
        .WithName("GetFixedAssets");

        assets.MapGet("/page", async (
            long companyId,
            int page,
            int pageSize,
            IFixedAssetService service,
            CancellationToken cancellationToken) =>
        {
            var workspace = await service.GetWorkspacePageAsync(companyId, page, pageSize, cancellationToken);
            return workspace is null
                ? Results.NotFound(new { error = "Company was not found in this workspace." })
                : Results.Ok(workspace);
        })
        .WithName("GetFixedAssetsPage");

        assets.MapPost("", async (
            long companyId,
            CreateFixedAssetRequest request,
            IFixedAssetService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var asset = await service.CreateAsync(companyId, request, cancellationToken);
                return asset is null
                    ? Results.NotFound(new { error = "Company was not found in this workspace." })
                    : Results.Created($"/api/v1/companies/{companyId}/fixed-assets/{asset.Id}", asset);
            }
            catch (FixedAssetValidationException exception) { return ValidationProblem(exception); }
            catch (FixedAssetConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("CreateFixedAsset");

        assets.MapPost("/{assetId:long}/depreciate", async (
            long companyId,
            long assetId,
            DepreciateFixedAssetRequest request,
            IFixedAssetService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.DepreciateAsync(companyId, assetId, request, cancellationToken));
            }
            catch (FixedAssetNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
            catch (FixedAssetValidationException exception) { return ValidationProblem(exception); }
            catch (FixedAssetConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("DepreciateFixedAsset");

        assets.MapPost("/{assetId:long}/dispose", async (
            long companyId,
            long assetId,
            DisposeFixedAssetRequest request,
            IFixedAssetService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.DisposeAsync(companyId, assetId, request, cancellationToken));
            }
            catch (FixedAssetNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
            catch (FixedAssetValidationException exception) { return ValidationProblem(exception); }
            catch (FixedAssetConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("DisposeFixedAsset");

        return app;
    }

    private static IResult ValidationProblem(FixedAssetValidationException exception) =>
        Results.ValidationProblem(exception.Errors);
}
