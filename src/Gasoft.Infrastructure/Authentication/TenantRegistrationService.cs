using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Gasoft.Application.Abstractions;
using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Authentication;

public sealed class TenantRegistrationService(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService) : ITenantRegistrationService
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant);
    private static readonly Regex CompanyCodePattern = new("^[A-Z0-9][A-Z0-9_-]{1,29}$", RegexOptions.CultureInvariant);

    public async Task<TenantRegistrationResult> RegisterAsync(
        TenantRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var tenantName = Normalize(request.TenantName);
        var slug = Normalize(request.Slug).ToLowerInvariant();
        var companyCode = Normalize(request.CompanyCode).ToUpperInvariant();
        var legalName = Normalize(request.LegalName);
        var currencyCode = Normalize(request.FunctionalCurrencyCode).ToUpperInvariant();
        var displayName = Normalize(request.OwnerDisplayName);
        var email = Normalize(request.Email);
        var password = request.Password ?? string.Empty;
        var errors = new Dictionary<string, string[]>();

        AddRequiredError(errors, "tenantName", tenantName, 200);
        if (slug.Length is 0 or > 100 || !SlugPattern.IsMatch(slug))
        {
            errors["slug"] = ["Use 1–100 lowercase letters, numbers, and hyphens; do not start or end with a hyphen."];
        }

        if (companyCode.Length is < 2 or > 30 || !CompanyCodePattern.IsMatch(companyCode))
        {
            errors["companyCode"] = ["Use 2–30 letters, numbers, hyphens, or underscores."];
        }

        AddRequiredError(errors, "legalName", legalName, 250);
        AddRequiredError(errors, "ownerDisplayName", displayName, 200);
        if (currencyCode.Length != 3 || currencyCode.Any(character => character is < 'A' or > 'Z'))
        {
            errors["functionalCurrencyCode"] = ["Enter a supported three-letter currency code, for example USD."];
        }

        if (string.IsNullOrWhiteSpace(email) || email.Length > 256 || !new EmailAddressAttribute().IsValid(email))
        {
            errors["email"] = ["Enter a valid email address (maximum 256 characters)."];
        }

        if (password.Length > 128)
        {
            errors["password"] = ["Password must be 128 characters or fewer."];
        }

        if (errors.Count != 0)
        {
            throw new RegistrationValidationException(errors);
        }

        if (await dbContext.Tenants.IgnoreQueryFilters().AnyAsync(tenant => tenant.Slug == slug, cancellationToken))
        {
            throw new RegistrationConflictException("That workspace address is already in use.");
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            throw new RegistrationConflictException("An account with that email address already exists.");
        }

        if (!await dbContext.Currencies.AnyAsync(currency => currency.Code == currencyCode, cancellationToken))
        {
            throw new RegistrationValidationException(new Dictionary<string, string[]>
            {
                ["functionalCurrencyCode"] = [$"Currency '{currencyCode}' is not currently supported."]
            });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var tenant = new Tenant { Name = tenantName, Slug = slug };
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName
            };
            var createUser = await userManager.CreateAsync(user, password);
            if (!createUser.Succeeded)
            {
                if (createUser.Errors.Any(error =>
                        error.Code is "DuplicateEmail" or "DuplicateUserName"))
                {
                    throw new RegistrationConflictException("An account with that email address already exists.");
                }

                throw new RegistrationValidationException(new Dictionary<string, string[]>
                {
                    ["password"] = createUser.Errors.Select(error => error.Description).ToArray()
                });
            }

            var company = new Company
            {
                TenantId = tenant.Id,
                Code = companyCode,
                LegalName = legalName,
                FunctionalCurrencyCode = currencyCode
            };
            dbContext.Companies.Add(company);
            dbContext.TenantUsers.Add(new TenantUser
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                IsActive = true,
                Role = "Owner"
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await CompanySetupSeeder.SeedLedgerDefaultsAsync(
                dbContext, tenant.Id, company, cancellationToken);

            var accessToken = tokenService.CreateToken(user.Id, user.Email!, tenant.Id, "Owner");
            await transaction.CommitAsync(cancellationToken);
            return new TenantRegistrationResult(tenant.Id, company.Id, accessToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new RegistrationConflictException("That workspace address, company code, or email is already in use.");
        }
    }

    private static void AddRequiredError(
        IDictionary<string, string[]> errors,
        string field,
        string value,
        int maxLength)
    {
        if (value.Length == 0 || value.Length > maxLength)
        {
            errors[field] = [$"This field is required and must be at most {maxLength} characters."];
        }
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
