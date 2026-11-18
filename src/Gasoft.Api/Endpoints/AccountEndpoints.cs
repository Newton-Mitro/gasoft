using System.Security.Claims;
using Gasoft.Application.Abstractions;
using Gasoft.Application.Authentication;
using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Persistence;

namespace Gasoft.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var account = app.MapGroup("/api/v1/account")
            .RequireAuthorization()
            .AddEndpointFilterFactory(RequestShapeValidationEndpointFilter.Create);

        account.MapGet("/profile", async (
            ClaimsPrincipal principal,
            IAccountService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            var profile = await service.GetProfileAsync(userId, cancellationToken);
            return profile is null ? Results.Unauthorized() : Results.Ok(profile);
        })
        .WithName("GetAccountProfile");

        account.MapPut("/profile", async (
            UpdateProfileRequest request,
            ClaimsPrincipal principal,
            IAccountService service,
            AppDbContext db,
            ICurrentTenant currentTenant,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            try
            {
                var profile = await service.UpdateProfileAsync(userId, request.DisplayName, cancellationToken);
                if (profile is null || currentTenant.TenantId is not long tenantId)
                    return Results.Unauthorized();
                db.AuditLogs.Add(new AuditLog
                {
                    TenantId = tenantId,
                    ActorUserId = userId,
                    Action = "ProfileUpdated",
                    EntityType = "UserProfile",
                    EntityId = userId.ToString(),
                    DetailsJson = "{\"fields\":[\"displayName\"]}",
                    CreatedAt = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync(cancellationToken);
                return Results.Ok(profile);
            }
            catch (AccountValidationException exception) { return ValidationProblem(exception); }
        })
        .WithName("UpdateAccountProfile");

        account.MapGet("/profile/image/{userId:long}/{fileName}", async (
            long userId,
            string fileName,
            ClaimsPrincipal principal,
            IAccountService service,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var currentUserId) || currentUserId != userId)
                return Results.NotFound();
            var profile = await service.GetProfileAsync(currentUserId, cancellationToken);
            if (profile is null ||
                profile.ProfileImageUrl != ImageUpload.ProfileImageUrl(userId, fileName))
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
            IAccountService service,
            AppDbContext db,
            ICurrentTenant currentTenant,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
            var currentProfile = await service.GetProfileAsync(userId, cancellationToken);
            if (currentProfile is null) return Results.Unauthorized();

            var savedImage = await ImageUpload.SaveAsync(
                image, environment, "users", userId.ToString(), cancellationToken);
            if (savedImage.Error is not null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["image"] = [savedImage.Error] });

            try
            {
                var profile = await service.UpdateProfileImageAsync(userId, savedImage.Url, cancellationToken);
                if (profile is null)
                {
                    ImageUpload.Delete(environment, savedImage.Url);
                    return Results.Unauthorized();
                }
                if (currentTenant.TenantId is not long tenantId)
                {
                    ImageUpload.Delete(environment, savedImage.Url);
                    return Results.Unauthorized();
                }
                db.AuditLogs.Add(new AuditLog
                {
                    TenantId = tenantId,
                    ActorUserId = userId,
                    Action = "ProfileUpdated",
                    EntityType = "UserProfile",
                    EntityId = userId.ToString(),
                    DetailsJson = "{\"fields\":[\"profileImageUrl\"]}",
                    CreatedAt = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync(cancellationToken);
                ImageUpload.Delete(environment, currentProfile.ProfileImageUrl);
                return Results.Ok(profile);
            }
            catch (AccountValidationException exception)
            {
                ImageUpload.Delete(environment, savedImage.Url);
                return ValidationProblem(exception);
            }
        })
        .DisableAntiforgery()
        .WithName("UploadAccountProfileImage");

        account.MapPost("/password", async (
            ChangePasswordRequest request,
            ClaimsPrincipal principal,
            IAccountService service,
            AppDbContext db,
            ICurrentTenant currentTenant,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["password"] = ["Enter your current password and a new password."]
                });
            if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();

            var errors = await service.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
            if (errors is not null) return Results.ValidationProblem(errors);
            if (currentTenant.TenantId is not long tenantId) return Results.Unauthorized();

            db.AuditLogs.Add(new AuditLog
            {
                TenantId = tenantId,
                ActorUserId = userId,
                Action = "PasswordChanged",
                EntityType = "Authentication",
                DetailsJson = "{\"result\":\"succeeded\"}",
                CreatedAt = DateTimeOffset.UtcNow
            });
            db.Notifications.Add(new AppNotification
            {
                TenantId = tenantId,
                UserId = userId,
                Title = "Password changed",
                Message = "Your account password was changed.",
                Category = "security",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        })
        .RequireRateLimiting("public-auth")
        .WithName("ChangeAccountPassword");

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out long userId) =>
        long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static IResult ValidationProblem(AccountValidationException exception) =>
        Results.ValidationProblem(exception.Errors);

    public sealed record UpdateProfileRequest(string? DisplayName);
    public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
}
