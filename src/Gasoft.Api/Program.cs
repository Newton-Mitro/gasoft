using System.Security.Claims;
using System.Text;
using Gasoft.Api.Endpoints;
using Gasoft.Infrastructure;
using Gasoft.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Configure Jwt:SigningKey using user-secrets or an environment variable.");

builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<BadRequestBodyExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(
            new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests",
                Detail = "Wait a minute before trying again."
            },
            cancellationToken);
    };
    options.AddPolicy("public-auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            NameClaimType = ClaimTypes.NameIdentifier,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantMembershipMiddleware>();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
app.MapAuthEndpoints();
app.MapAccountEndpoints();
app.MapTenantRegistrationEndpoints();
app.MapPartyEndpoints();
app.MapAccountingEndpoints();
app.MapBillingEndpoints();
app.MapSalesQuoteEndpoints();
app.MapInventoryEndpoints();
app.MapReportingEndpoints();
app.MapCompanyAdministrationEndpoints();
app.MapTenantUserEndpoints();
app.MapTaxEndpoints();
app.MapBankReconciliationEndpoints();
app.MapConsolidationEndpoints();
app.MapPurchaseOrderEndpoints();
app.MapBudgetEndpoints();
app.MapFixedAssetEndpoints();
app.MapNotificationAndAuditEndpoints();

app.Run();

public partial class Program;
