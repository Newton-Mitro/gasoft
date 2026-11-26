using Gasoft.Application.Abstractions;
using Gasoft.Application.Billing;
using Gasoft.Application.Common;
using Gasoft.Application.Inventory;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class BillingService(
    AppDbContext db,
    ICurrentTenant currentTenant,
    IInventoryService inventory) : IBillingService
{
    private const decimal MaximumAmount = 99_999_999_999_999.999999m;
}
