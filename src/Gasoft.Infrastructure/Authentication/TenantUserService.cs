using System.ComponentModel.DataAnnotations;
using Gasoft.Application.Abstractions;
using Gasoft.Application.TenantUsers;
using Gasoft.Application.Common;
using Gasoft.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Authentication;

public sealed class TenantUserService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager) : ITenantUserService
{
    private static readonly string[] AssignableRoles = ["Admin", "Member"];

    public async Task<string?> GetActiveMembershipRoleAsync(
        long tenantId,
        long userId,
        CancellationToken cancellationToken)
    {
        var role = await db.TenantUsers
            .Where(item => item.TenantId == tenantId && item.UserId == userId && item.IsActive)
            .Select(item => item.Role)
            .SingleOrDefaultAsync(cancellationToken);
        if (role is null || !await db.Tenants.IgnoreQueryFilters()
                .AnyAsync(item => item.Id == tenantId && item.Status == "active", cancellationToken))
            return null;
        return role;
    }

    public async Task<TenantManagerDto?> GetManagerAsync(
        long tenantId,
        long userId,
        CancellationToken cancellationToken)
    {
        var role = await db.TenantUsers
            .Where(membership =>
                membership.TenantId == tenantId &&
                membership.UserId == userId &&
                membership.IsActive &&
                (membership.Role == "Owner" || membership.Role == "Admin"))
            .Select(membership => membership.Role)
            .SingleOrDefaultAsync(cancellationToken);
        return role is null ? null : new TenantManagerDto(tenantId, role);
    }

    public async Task<IReadOnlyList<TenantUserDto>> GetUsersAsync(
        long tenantId,
        CancellationToken cancellationToken)
    {
        var users = await db.TenantUsers
            .Where(membership => membership.TenantId == tenantId)
            .Join(db.Users, membership => membership.UserId, user => user.Id, (membership, user) =>
                new TenantUserDto(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.DisplayName ?? string.Empty,
                    membership.Role,
                    membership.IsActive))
            .ToListAsync(cancellationToken);
        return users
            .OrderBy(user => user.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(user => user.Email, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public Task<PagedResultDto<TenantUserDto>> GetUsersPageAsync(
        long tenantId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var users = db.TenantUsers
            .Where(membership => membership.TenantId == tenantId)
            .Join(db.Users, membership => membership.UserId, user => user.Id, (membership, user) =>
                new TenantUserDto(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.DisplayName ?? string.Empty,
                    membership.Role,
                    membership.IsActive))
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email);
        return PagedQuery.ToPageAsync(users, page, pageSize, user => user, cancellationToken);
    }

    public Task<TenantUserDto?> GetUserAsync(
        long tenantId,
        long userId,
        CancellationToken cancellationToken) =>
        db.TenantUsers
            .Where(membership => membership.TenantId == tenantId && membership.UserId == userId)
            .Join(db.Users, membership => membership.UserId, user => user.Id, (membership, user) =>
                new TenantUserDto(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.DisplayName ?? string.Empty,
                    membership.Role,
                    membership.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<TenantUserDto> CreateAsync(
        TenantManagerDto manager,
        CreateTenantUserRequest request,
        CancellationToken cancellationToken)
    {
        var (email, displayName, password, role) = ValidateNewUserRequest(manager, request);
        if (await userManager.FindByEmailAsync(email) is not null)
            throw new TenantUserConflictException("An account with that email address already exists.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName
        };
        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new TenantUserValidationException(new Dictionary<string, string[]>
            {
                ["temporaryPassword"] = createResult.Errors.Select(error => error.Description).ToArray()
            });
        }

        var membership = new TenantUser
        {
            TenantId = manager.TenantId,
            UserId = user.Id,
            Role = role,
            IsActive = true
        };
        db.TenantUsers.Add(membership);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new TenantUserConflictException("An account with that email address already exists.");
        }
        return ToDto(user, membership);
    }

    public async Task<TenantUserDto> AddExistingAsync(
        TenantManagerDto manager,
        AddExistingTenantUserRequest request,
        CancellationToken cancellationToken)
    {
        var (email, role) = ValidateExistingUserRequest(manager, request);
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            throw new TenantUserNotFoundException("No existing account was found with that email address.");
        if (await db.TenantUsers.AnyAsync(
                membership => membership.TenantId == manager.TenantId && membership.UserId == user.Id,
                cancellationToken))
            throw AlreadyMember();

        var membership = new TenantUser
        {
            TenantId = manager.TenantId,
            UserId = user.Id,
            Role = role,
            IsActive = true
        };
        db.TenantUsers.Add(membership);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsTenantMembershipConflict(exception))
        {
            throw AlreadyMember();
        }
        return ToDto(user, membership, email);
    }

    public async Task<TenantUserDto?> UpdateAsync(
        TenantManagerDto manager,
        long userId,
        UpdateTenantUserRequest request,
        CancellationToken cancellationToken)
    {
        var membership = await db.TenantUsers.SingleOrDefaultAsync(
            item => item.TenantId == manager.TenantId && item.UserId == userId,
            cancellationToken);
        if (membership is null) return null;

        var role = NormalizeRole(request.Role);
        if (role is null || (role == "Owner" && manager.Role != "Owner"))
            throw RoleValidation();
        if (membership.Role == "Owner" && manager.Role != "Owner")
            throw new TenantUserForbiddenException("Only an Owner can change another Owner's access.");

        var isChangingOwnerStatus = membership.Role == "Owner" &&
                                    membership.IsActive &&
                                    (!request.IsActive || role != "Owner");
        if (isChangingOwnerStatus &&
            !await db.TenantUsers.AnyAsync(
                item => item.TenantId == manager.TenantId &&
                        item.Role == "Owner" &&
                        item.IsActive &&
                        item.UserId != userId,
                cancellationToken))
            throw new TenantUserConflictException("The tenant must have at least one active owner.");

        membership.Role = role;
        membership.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        var user = await db.Users.AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new { item.Email, item.DisplayName })
            .SingleAsync(cancellationToken);
        return new TenantUserDto(
            userId,
            user.Email ?? string.Empty,
            user.DisplayName ?? string.Empty,
            membership.Role,
            membership.IsActive);
    }

    private static (string Email, string DisplayName, string Password, string Role) ValidateNewUserRequest(
        TenantManagerDto manager,
        CreateTenantUserRequest request)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        var displayName = request.DisplayName?.Trim() ?? string.Empty;
        var password = request.TemporaryPassword ?? string.Empty;
        var role = NormalizeRole(request.Role);
        var errors = new Dictionary<string, string[]>();
        if (email.Length > 256 || !new EmailAddressAttribute().IsValid(email))
            errors["email"] = ["Enter a valid email address (maximum 256 characters)."];
        if (displayName.Length is 0 or > 200)
            errors["displayName"] = ["Display name is required and must be at most 200 characters."];
        if (password.Length is 0 or > 128)
            errors["temporaryPassword"] = ["Temporary password is required and must be at most 128 characters."];
        if (role is null || (role == "Owner" && manager.Role != "Owner"))
            errors["role"] = ["Choose a role you are permitted to assign."];
        if (errors.Count != 0) throw new TenantUserValidationException(errors);
        return (email, displayName, password, role!);
    }

    private static (string Email, string Role) ValidateExistingUserRequest(
        TenantManagerDto manager,
        AddExistingTenantUserRequest request)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        var role = NormalizeRole(request.Role);
        var errors = new Dictionary<string, string[]>();
        if (email.Length > 256 || !new EmailAddressAttribute().IsValid(email))
            errors["email"] = ["Enter a valid email address (maximum 256 characters)."];
        if (role is null || (role == "Owner" && manager.Role != "Owner"))
            errors["role"] = ["Choose a role you are permitted to assign."];
        if (errors.Count != 0) throw new TenantUserValidationException(errors);
        return (email, role!);
    }

    private static string? NormalizeRole(string? role)
    {
        var normalized = role?.Trim();
        return normalized == "Owner" || AssignableRoles.Contains(normalized, StringComparer.OrdinalIgnoreCase)
            ? normalized == "Owner"
                ? "Owner"
                : AssignableRoles.Single(item => item.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            : null;
    }

    private static TenantUserValidationException RoleValidation() =>
        new(new Dictionary<string, string[]>
        {
            ["role"] = ["Choose a role you are permitted to assign."]
        });

    private static TenantUserConflictException AlreadyMember() =>
        new("This account is already a member of the workspace. Reactivate it from the team member list if needed.");

    private static TenantUserDto ToDto(ApplicationUser user, TenantUser membership, string? fallbackEmail = null) =>
        new(
            user.Id,
            user.Email ?? fallbackEmail ?? string.Empty,
            user.DisplayName ?? string.Empty,
            membership.Role,
            membership.IsActive);

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static bool IsTenantMembershipConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "PK_TenantUsers"
        };
}
