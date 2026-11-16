using Gasoft.Application.Abstractions;
using Gasoft.Infrastructure.Authentication;
using Gasoft.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gasoft.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Default.");
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenant, CurrentTenant>();
        services.AddScoped<IPartyService, PartyService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITenantRegistrationService, TenantRegistrationService>();
        services.AddScoped<IAccountingService, AccountingService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<ICompanyReadService, CompanyReadService>();
        services.AddScoped<ISalesQuoteService, SalesQuoteService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<ITenantUserService, TenantUserService>();
        services.AddScoped<ICompanyAdministrationService, CompanyAdministrationService>();
        services.AddScoped<ITaxService, TaxService>();
        services.AddScoped<IFixedAssetService, FixedAssetService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IConsolidationService, ConsolidationService>();
        services.AddScoped<IBankReconciliationService, BankReconciliationService>();
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<long>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromMinutes(30));
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName));
        services.AddTransient<IEmailSender, SmtpEmailSender>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.Issuer) &&
                !string.IsNullOrWhiteSpace(options.Audience) &&
                !string.IsNullOrWhiteSpace(options.SigningKey) &&
                EncodingByteCount(options.SigningKey) >= 32 &&
                options.ExpiresInMinutes is > 0 and <= 1440,
                "JWT issuer, audience, a signing key of at least 32 bytes, and a valid expiry are required.")
            .ValidateOnStart();

        return services;
    }

    private static int EncodingByteCount(string value) => System.Text.Encoding.UTF8.GetByteCount(value);
}
