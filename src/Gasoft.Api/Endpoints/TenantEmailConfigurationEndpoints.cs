using System.Net.Mail;
using Gasoft.Application.Abstractions;
using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Api.Endpoints;

public static class TenantEmailConfigurationEndpoints
{
    public static IEndpointRouteBuilder MapTenantEmailConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var emailSettings = app.MapGroup("/api/v1/settings/email")
            .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"));

        emailSettings.MapGet("", async (
            AppDbContext db,
            ICurrentTenant currentTenant,
            CancellationToken cancellationToken) =>
        {
            if (currentTenant.TenantId is not long tenantId)
                return Results.BadRequest(new { error = "Choose a tenant before viewing email settings." });

            var configuration = await db.TenantEmailConfigurations.IgnoreQueryFilters()
                .SingleOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken);
            return Results.Ok(new TenantEmailConfigurationDto(
                configuration?.SmtpHost ?? string.Empty,
                configuration?.SmtpPort ?? 587,
                configuration?.SmtpUsername ?? string.Empty,
                configuration?.FromEmail ?? string.Empty,
                configuration?.FromName ?? "Gasoft",
                !string.IsNullOrEmpty(configuration?.ProtectedSmtpPassword)));
        })
        .WithName("GetTenantEmailConfiguration");

        emailSettings.MapPut("", async (
            UpdateTenantEmailConfigurationRequest request,
            AppDbContext db,
            ICurrentTenant currentTenant,
            IEmailPasswordProtector passwordProtector,
            CancellationToken cancellationToken) =>
        {
            if (currentTenant.TenantId is not long tenantId)
                return Results.BadRequest(new { error = "Choose a tenant before saving email settings." });

            var errors = Validate(request);
            var configuration = await db.TenantEmailConfigurations.IgnoreQueryFilters()
                .SingleOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken);
            var passwordWillBeSet = !string.IsNullOrEmpty(request.SmtpPassword) ||
                !request.ClearPassword && !string.IsNullOrEmpty(configuration?.ProtectedSmtpPassword);
            if (!string.IsNullOrWhiteSpace(request.SmtpUsername) && !passwordWillBeSet)
                errors["smtpPassword"] = ["Enter the SMTP password or clear the username and password."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            configuration ??= new TenantEmailConfiguration
            {
                TenantId = tenantId,
                SmtpHost = string.Empty,
                SmtpPort = 587,
                FromEmail = string.Empty,
                FromName = "Gasoft"
            };
            configuration.SmtpHost = request.SmtpHost.Trim();
            configuration.SmtpPort = request.SmtpPort;
            configuration.SmtpUsername = string.IsNullOrWhiteSpace(request.SmtpUsername)
                ? null
                : request.SmtpUsername.Trim();
            configuration.FromEmail = request.FromEmail.Trim();
            configuration.FromName = request.FromName.Trim();
            configuration.UpdatedAt = DateTimeOffset.UtcNow;

            if (!string.IsNullOrEmpty(request.SmtpPassword))
                configuration.ProtectedSmtpPassword = passwordProtector.Protect(request.SmtpPassword);
            else if (request.ClearPassword)
                configuration.ProtectedSmtpPassword = null;

            if (db.Entry(configuration).State == EntityState.Detached)
                db.TenantEmailConfigurations.Add(configuration);

            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        })
        .WithName("SaveTenantEmailConfiguration");

        return app;
    }

    private static Dictionary<string, string[]> Validate(UpdateTenantEmailConfigurationRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var host = request.SmtpHost?.Trim() ?? string.Empty;
        var fromEmail = request.FromEmail?.Trim() ?? string.Empty;
        var fromName = request.FromName?.Trim() ?? string.Empty;

        if (host.Length is 0 or > 253 ||
            Uri.CheckHostName(host) == UriHostNameType.Unknown ||
            host.Any(char.IsWhiteSpace) ||
            host.Any(char.IsControl))
            errors["smtpHost"] = ["Enter a valid SMTP host name."];
        if (request.SmtpPort is < 1 or > 65535)
            errors["smtpPort"] = ["Enter a port between 1 and 65535."];
        if ((request.SmtpUsername?.Length ?? 0) > 200)
            errors["smtpUsername"] = ["The SMTP username must be 200 characters or fewer."];
        if ((request.SmtpPassword?.Length ?? 0) > 1024)
            errors["smtpPassword"] = ["The SMTP password must be 1024 characters or fewer."];
        if (request.ClearPassword && !string.IsNullOrEmpty(request.SmtpPassword))
            errors["smtpPassword"] = ["Enter a new password or choose to clear the saved password, not both."];
        if (!MailAddress.TryCreate(fromEmail, out var parsedAddress) ||
            !string.Equals(parsedAddress.Address, fromEmail, StringComparison.OrdinalIgnoreCase))
            errors["fromEmail"] = ["Enter a valid sender email address."];
        if (fromName.Length is 0 or > 200 || fromName.Any(char.IsControl))
            errors["fromName"] = ["Enter a sender name of 1 to 200 characters without control characters."];

        return errors;
    }

    private sealed record TenantEmailConfigurationDto(
        string SmtpHost,
        int SmtpPort,
        string SmtpUsername,
        string FromEmail,
        string FromName,
        bool HasPassword);

    private sealed record UpdateTenantEmailConfigurationRequest(
        string SmtpHost,
        int SmtpPort,
        string? SmtpUsername,
        string? SmtpPassword,
        string FromEmail,
        string FromName,
        bool ClearPassword);
}
