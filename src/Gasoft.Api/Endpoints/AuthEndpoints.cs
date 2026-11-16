using Gasoft.Application.Abstractions;

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
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            if (!await authService.RequestPasswordResetAsync(
                    request.Email,
                    request.WorkspaceSlug,
                    cancellationToken))
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Password recovery is not available",
                    detail: "Password reset email is not configured. Contact your administrator.");

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
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var errors = await authService.ResetPasswordAsync(
                request.Email,
                request.Token,
                request.NewPassword,
                cancellationToken);
            return errors is null ? Results.NoContent() : Results.ValidationProblem(errors);
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
