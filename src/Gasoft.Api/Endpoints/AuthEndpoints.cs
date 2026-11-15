using System.Net.Mail;
using Gasoft.Application.Abstractions;
using Gasoft.Infrastructure.Authentication;
using Gasoft.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gasoft.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/token", async (
            LoginRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var workspace = await authService.AuthenticateAsync(
                request.Email,
                request.Password,
                request.WorkspaceSlug,
                cancellationToken);

            return workspace is null
                ? Results.Unauthorized()
                : Results.Ok(new TokenResponse(
                    workspace.AccessToken,
                    workspace.TenantId,
                    workspace.CompanyId));
        })
        .AllowAnonymous()
        .RequireRateLimiting("public-auth")
        .WithName("CreateAccessToken");

        app.MapPost("/api/v1/auth/password/forgot", async (
            ForgotPasswordRequest request,
            IEmailSender emailSender,
            IOptions<EmailOptions> emailOptions,
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            if (!emailSender.IsConfigured)
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Password recovery is not available",
                    detail: "Password reset email is not configured. Contact your administrator.");

            var email = request.Email?.Trim() ?? string.Empty;
            var slug = request.WorkspaceSlug?.Trim().ToLowerInvariant() ?? string.Empty;
            var tenantId = await db.Tenants
                .IgnoreQueryFilters()
                .Where(tenant => tenant.Slug == slug && tenant.Status == "active")
                .Select(tenant => (long?)tenant.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (tenantId is not null)
            {
                var user = await userManager.FindByEmailAsync(email);
                var isActiveMember = user is not null && await db.TenantUsers
                    .IgnoreQueryFilters()
                    .AnyAsync(membership =>
                        membership.TenantId == tenantId &&
                        membership.UserId == user.Id &&
                        membership.IsActive,
                        cancellationToken);

                if (user is not null && isActiveMember)
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var resetLink = $"{emailOptions.Value.FrontendBaseUrl.TrimEnd('/')}/reset-password" +
                        $"?email={Uri.EscapeDataString(user.Email ?? email)}&token={Uri.EscapeDataString(token)}";
                    try
                    {
                        await emailSender.SendPasswordResetAsync(user.Email ?? email, resetLink, cancellationToken);
                    }
                    catch (SmtpException exception)
                    {
                        loggerFactory.CreateLogger("PasswordReset")
                            .LogError(exception, "Could not deliver a password reset email.");
                    }
                }
            }

            return Results.Ok(new
            {
                message = "If an account matches those details, a password reset link will be sent shortly."
            });
        })
        .AllowAnonymous()
        .RequireRateLimiting("public-auth")
        .WithName("RequestPasswordReset");

        app.MapPost("/api/v1/auth/password/reset", async (
            ResetPasswordRequest request,
            UserManager<ApplicationUser> userManager) =>
        {
            var email = request.Email?.Trim() ?? string.Empty;
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["token"] = ["This reset link is invalid or has expired. Request a new one."]
                });

            var result = await userManager.ResetPasswordAsync(
                user,
                request.Token ?? string.Empty,
                request.NewPassword ?? string.Empty);
            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .Select(error => error.Code == "InvalidToken"
                        ? ("token", "This reset link is invalid or has expired. Request a new one.")
                        : ("newPassword", error.Description))
                    .GroupBy(error => error.Item1)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.Item2).ToArray());
                return Results.ValidationProblem(errors);
            }

            return Results.NoContent();
        })
        .AllowAnonymous()
        .RequireRateLimiting("public-auth")
        .WithName("ResetPassword");

        return app;
    }

    public sealed record LoginRequest(string Email, string Password, string WorkspaceSlug);
    public sealed record TokenResponse(string AccessToken, long TenantId, long CompanyId);
    public sealed record ForgotPasswordRequest(string? Email, string? WorkspaceSlug);
    public sealed record ResetPasswordRequest(string? Email, string? Token, string? NewPassword);
}
