using Gasoft.Application.Abstractions;
using Gasoft.Application.Companies;

namespace Gasoft.Api.Endpoints;

public static class CompanyAdministrationEndpoints
{
    public static IEndpointRouteBuilder MapCompanyAdministrationEndpoints(this IEndpointRouteBuilder app)
    {
        var tenantCompanies = app.MapGroup("/api/v1/tenant/companies")
            .RequireAuthorization()
            .AddEndpointFilterFactory(RequestShapeValidationEndpointFilter.Create);

        tenantCompanies.MapGet("", async (
            ICompanyAdministrationService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.GetCompaniesAsync(cancellationToken)))
            .WithName("GetTenantCompanies");

        tenantCompanies.MapPost("", async (
            CreateCompanyRequest request,
            ICompanyAdministrationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var company = await service.CreateCompanyAsync(request, cancellationToken);
                return company is null
                    ? Results.Unauthorized()
                    : Results.Created($"/api/v1/companies/{company.Id}", company);
            }
            catch (CompanyValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
            catch (CompanyConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("CreateTenantCompany");

        app.MapGet("/api/v1/companies/{companyId:long}/logo/{fileName}", async (
            long companyId,
            string fileName,
            ICompanyAdministrationService service,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            var company = await service.GetCompanyAsync(companyId, cancellationToken);
            if (company?.LogoUrl != ImageUpload.CompanyLogoUrl(companyId, fileName)) return Results.NotFound();

            var image = ImageUpload.OpenRead(environment, "companies", companyId.ToString(), fileName);
            return image.Stream is null || image.ContentType is null
                ? Results.NotFound()
                : Results.File(image.Stream, image.ContentType);
        })
        .RequireAuthorization()
        .WithName("GetCompanyLogo");

        app.MapPost("/api/v1/companies/{companyId:long}/logo", async (
            long companyId,
            IFormFile image,
            ICompanyAdministrationService service,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            var existingCompany = await service.GetCompanyAsync(companyId, cancellationToken);
            if (existingCompany is null)
                return Results.NotFound(new { error = "Company was not found in this tenant." });

            var savedImage = await ImageUpload.SaveAsync(
                image, environment, "companies", companyId.ToString(), cancellationToken);
            if (savedImage.Error is not null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["image"] = [savedImage.Error] });

            CompanyDto? company;
            try
            {
                company = await service.SetCompanyLogoUrlAsync(companyId, savedImage.Url!, cancellationToken);
            }
            catch
            {
                ImageUpload.Delete(environment, savedImage.Url!);
                throw;
            }

            if (company is null)
            {
                ImageUpload.Delete(environment, savedImage.Url!);
                return Results.NotFound(new { error = "Company was not found in this tenant." });
            }

            ImageUpload.Delete(environment, existingCompany.LogoUrl);
            return Results.Ok(company);
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .DisableAntiforgery()
        .WithName("UploadCompanyLogo");

        app.MapPut("/api/v1/companies/{companyId:long}", async (
            long companyId,
            UpdateCompanyRequest request,
            ICompanyAdministrationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var company = await service.UpdateCompanyAsync(companyId, request, cancellationToken);
                return company is null
                    ? Results.NotFound(new { error = "Company was not found in this tenant." })
                    : Results.Ok(company);
            }
            catch (CompanyValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
            catch (CompanyConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .AddEndpointFilterFactory(RequestShapeValidationEndpointFilter.Create)
        .WithName("UpdateCompany");

        app.MapGet("/api/v1/companies/{companyId:long}/export", async (
            long companyId,
            ICompanyDataExportService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var export = await service.ExportAsync(companyId, cancellationToken);
            if (export is null)
            {
                return Results.NotFound(new { error = "Company was not found in this tenant." });
            }

            httpContext.Response.Headers.CacheControl = "no-store";
            httpContext.Response.Headers.Pragma = "no-cache";
            return Results.File(export.Content, "application/zip", export.FileName);
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("ExportCompanyData");

        return app;
    }
}
