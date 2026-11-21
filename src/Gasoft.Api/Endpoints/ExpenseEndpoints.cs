using System.Security.Claims;
using Gasoft.Application.Abstractions;
using Gasoft.Application.Accounting;
using Gasoft.Application.Expenses;

namespace Gasoft.Api.Endpoints;

public static class ExpenseEndpoints
{
    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var reports = app.MapGroup("/api/v1/companies/{companyId:long}/expense-reports")
            .RequireAuthorization()
            .AddEndpointFilterFactory(RequestShapeValidationEndpointFilter.Create);

        reports.MapGet("", async (
            long companyId,
            ClaimsPrincipal principal,
            IExpenseReportService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetReportsAsync(
                    companyId, CanReview(principal), cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .WithName("GetExpenseReports");

        reports.MapGet("/{reportId:long}", async (
            long companyId,
            long reportId,
            ClaimsPrincipal principal,
            IExpenseReportService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var report = await service.GetReportAsync(
                    companyId, reportId, CanReview(principal), cancellationToken);
                return report is null ? Results.NotFound() : Results.Ok(report);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .WithName("GetExpenseReport");

        reports.MapPost("", async (
            long companyId,
            CreateExpenseReportRequest request,
            IExpenseReportService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var report = await service.CreateReportAsync(companyId, request, cancellationToken);
                return Results.Created($"/api/v1/companies/{companyId}/expense-reports/{report.Id}", report);
            }
            catch (ExpenseReportValidationException exception) { return ValidationProblem(exception); }
            catch (ExpenseReportConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .WithName("CreateExpenseReport");

        reports.MapPut("/{reportId:long}", async (
            long companyId,
            long reportId,
            CreateExpenseReportRequest request,
            IExpenseReportService service,
            IWebHostEnvironment environment,
            ICurrentTenant currentTenant,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.UpdateReportAsync(companyId, reportId, request, cancellationToken);
                if (result is null)
                    return Results.NotFound();
                if (currentTenant.TenantId is not long tenantId)
                    return Results.Forbid();
                foreach (var receipt in result.RemovedReceipts)
                {
                    ExpenseReceiptStorage.Delete(
                        environment, tenantId, companyId, receipt.ReportId, receipt.LineId, receipt.FileName);
                }

                return Results.Ok(result.Report);
            }
            catch (ExpenseReportValidationException exception) { return ValidationProblem(exception); }
            catch (ExpenseReportConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .WithName("UpdateExpenseReport");

        reports.MapPost("/{reportId:long}/submit", async (
            long companyId,
            long reportId,
            IExpenseReportService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var report = await service.SubmitReportAsync(companyId, reportId, cancellationToken);
                return report is null ? Results.NotFound() : Results.Ok(report);
            }
            catch (ExpenseReportValidationException exception) { return ValidationProblem(exception); }
            catch (ExpenseReportConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .WithName("SubmitExpenseReport");

        reports.MapPost("/{reportId:long}/approve", async (
            long companyId,
            long reportId,
            ReviewExpenseReportRequest request,
            IExpenseReportService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var report = await service.ReviewReportAsync(
                    companyId, reportId, approve: true, request, cancellationToken);
                return report is null ? Results.NotFound() : Results.Ok(report);
            }
            catch (ExpenseReportValidationException exception) { return ValidationProblem(exception); }
            catch (ExpenseReportConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("ApproveExpenseReport");

        reports.MapPost("/{reportId:long}/reject", async (
            long companyId,
            long reportId,
            ReviewExpenseReportRequest request,
            IExpenseReportService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var report = await service.ReviewReportAsync(
                    companyId, reportId, approve: false, request, cancellationToken);
                return report is null ? Results.NotFound() : Results.Ok(report);
            }
            catch (ExpenseReportValidationException exception) { return ValidationProblem(exception); }
            catch (ExpenseReportConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("RejectExpenseReport");

        reports.MapPost("/{reportId:long}/post", async (
            long companyId,
            long reportId,
            PostExpenseReportRequest request,
            IExpenseReportService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var report = await service.PostReportAsync(companyId, reportId, request, cancellationToken);
                return report is null ? Results.NotFound() : Results.Ok(report);
            }
            catch (ExpenseReportValidationException exception) { return ValidationProblem(exception); }
            catch (AccountingValidationException exception) { return Results.ValidationProblem(exception.Errors); }
            catch (AccountingConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (ExpenseReportConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("PostExpenseReport");

        reports.MapPost("/{reportId:long}/reimburse", async (
            long companyId,
            long reportId,
            ReimburseExpenseReportRequest request,
            IExpenseReportService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var report = await service.ReimburseReportAsync(companyId, reportId, request, cancellationToken);
                return report is null ? Results.NotFound() : Results.Ok(report);
            }
            catch (ExpenseReportValidationException exception) { return ValidationProblem(exception); }
            catch (AccountingValidationException exception) { return Results.ValidationProblem(exception.Errors); }
            catch (AccountingConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (ExpenseReportConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("ReimburseExpenseReport");

        reports.MapPost("/{reportId:long}/lines/{lineId:long}/receipt", async (
            long companyId,
            long reportId,
            long lineId,
            IFormFile receipt,
            ClaimsPrincipal principal,
            IExpenseReportService service,
            ICurrentTenant currentTenant,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            var userCanReview = CanReview(principal);
            ExpenseReportDto? report;
            try
            {
                report = await service.GetReportAsync(companyId, reportId, userCanReview, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            if (report is null || !report.CanEdit || report.Lines.All(line => line.Id != lineId))
                return Results.NotFound();
            if (currentTenant.TenantId is not long tenantId)
                return Results.Forbid();

            var saved = await ExpenseReceiptStorage.SaveAsync(
                receipt, environment, tenantId, companyId, reportId, lineId, cancellationToken);
            if (saved.FileName is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["receipt"] = [saved.Error ?? "The receipt could not be saved."]
                });
            }

            try
            {
                var previousFileName = await service.GetReceiptFileNameAsync(
                    companyId, reportId, lineId, includeAll: false, cancellationToken);
                var updated = await service.SetReceiptAsync(
                    companyId, reportId, lineId, saved.FileName, userCanReview, cancellationToken);
                if (updated is null)
                {
                    ExpenseReceiptStorage.Delete(
                        environment, tenantId, companyId, reportId, lineId, saved.FileName);
                    return Results.NotFound();
                }
                if (previousFileName is not null && previousFileName != saved.FileName)
                {
                    ExpenseReceiptStorage.Delete(
                        environment, tenantId, companyId, reportId, lineId, previousFileName);
                }

                return Results.Ok(updated);
            }
            catch (ExpenseReportConflictException exception)
            {
                ExpenseReceiptStorage.Delete(environment, tenantId, companyId, reportId, lineId, saved.FileName);
                return Results.Conflict(new { error = exception.Message });
            }
            catch (UnauthorizedAccessException)
            {
                ExpenseReceiptStorage.Delete(environment, tenantId, companyId, reportId, lineId, saved.FileName);
                return Results.Forbid();
            }
        })
        .DisableAntiforgery()
        .WithName("UploadExpenseReceipt");

        reports.MapGet("/{reportId:long}/lines/{lineId:long}/receipt/{fileName}", async (
            long companyId,
            long reportId,
            long lineId,
            string fileName,
            ClaimsPrincipal principal,
            IExpenseReportService service,
            ICurrentTenant currentTenant,
            IWebHostEnvironment environment,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            string? storedFileName;
            try
            {
                storedFileName = await service.GetReceiptFileNameAsync(
                    companyId, reportId, lineId, CanReview(principal), cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            if (storedFileName is null || !string.Equals(storedFileName, fileName, StringComparison.Ordinal))
                return Results.NotFound();
            if (currentTenant.TenantId is not long tenantId)
                return Results.Forbid();

            var receipt = ExpenseReceiptStorage.OpenRead(
                environment, tenantId, companyId, reportId, lineId, storedFileName);
            if (receipt.Stream is null || receipt.ContentType is null)
                return Results.NotFound();
            httpContext.Response.Headers.XContentTypeOptions = "nosniff";
            httpContext.Response.Headers.CacheControl = "private, no-store";
            return Results.File(receipt.Stream, receipt.ContentType, fileDownloadName: fileName);
        })
        .WithName("GetExpenseReceipt");

        return app;
    }

    private static bool CanReview(ClaimsPrincipal principal) =>
        principal.IsInRole("Owner") || principal.IsInRole("Admin");

    private static IResult ValidationProblem(ExpenseReportValidationException exception) =>
        Results.ValidationProblem(exception.Errors);
}
