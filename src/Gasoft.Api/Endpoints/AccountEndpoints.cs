using System.Security.Claims;
using Gasoft.Infrastructure.Authentication;
using Gasoft.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var account = app.MapGroup("/api/v1/account").RequireAuthorization();

        account.MapGet("/profile", async (
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            return user is null
                ? Results.Unauthorized()
                : Results.Ok(new ProfileResponse(user.DisplayName ?? string.Empty, user.Email ?? string.Empty));
        })
        .WithName("GetAccountProfile");

        account.MapPut("/profile", async (
            UpdateProfileRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            var displayName = request.DisplayName?.Trim() ?? string.Empty;
            if (displayName.Length is 0 or > 200)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["displayName"] = ["Display name is required and must be at most 200 characters."]
                });

            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();

            user.DisplayName = displayName;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["displayName"] = result.Errors.Select(error => error.Description).ToArray()
                });

            return Results.Ok(new ProfileResponse(user.DisplayName, user.Email ?? string.Empty));
        })
        .WithName("UpdateAccountProfile");

        account.MapPost("/password", async (
            ChangePasswordRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["password"] = ["Enter your current password and a new password."]
                });

            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .Select(error => error.Code == "PasswordMismatch"
                        ? ("currentPassword", "Your current password is incorrect.")
                        : ("newPassword", error.Description))
                    .GroupBy(error => error.Item1)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.Item2).ToArray());
                return Results.ValidationProblem(errors);
            }

            return Results.NoContent();
        })
        .RequireRateLimiting("public-auth")
        .WithName("ChangeAccountPassword");

        return app;
    }

    public sealed record ProfileResponse(string DisplayName, string Email);
    public sealed record UpdateProfileRequest(string? DisplayName);
    public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
}
