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
                : Results.Ok(new ProfileResponse(user.DisplayName ?? string.Empty, user.Email ?? string.Empty, user.ProfileImageUrl));
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

            return Results.Ok(new ProfileResponse(user.DisplayName, user.Email ?? string.Empty, user.ProfileImageUrl));
        })
        .WithName("UpdateAccountProfile");

        account.MapGet("/profile/image/{userId:long}/{fileName}", async (
            long userId,
            string fileName,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null || user.Id != userId ||
                user.ProfileImageUrl != ImageUpload.ProfileImageUrl(userId, fileName))
                return Results.NotFound();

            var image = ImageUpload.OpenRead(environment, "users", userId.ToString(), fileName);
            return image.Stream is null || image.ContentType is null
                ? Results.NotFound()
                : Results.File(image.Stream, image.ContentType);
        })
        .WithName("GetAccountProfileImage");

        account.MapPost("/profile/image", async (
            IFormFile image,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();

            var savedImage = await ImageUpload.SaveAsync(image, environment, "users", user.Id.ToString(), cancellationToken);
            if (savedImage.Error is not null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["image"] = [savedImage.Error] });

            var previousImageUrl = user.ProfileImageUrl;
            user.ProfileImageUrl = savedImage.Url;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ImageUpload.Delete(environment, savedImage.Url!);
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["image"] = result.Errors.Select(error => error.Description).ToArray()
                });
            }

            ImageUpload.Delete(environment, previousImageUrl);
            return Results.Ok(new ProfileResponse(user.DisplayName ?? string.Empty, user.Email ?? string.Empty, user.ProfileImageUrl));
        })
        .DisableAntiforgery()
        .WithName("UploadAccountProfileImage");

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

    public sealed record ProfileResponse(string DisplayName, string Email, string? ProfileImageUrl);
    public sealed record UpdateProfileRequest(string? DisplayName);
    public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
}
