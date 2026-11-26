using Gasoft.Application.Abstractions;
using Gasoft.Application.Accounting;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Gasoft.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class AccountingService : AccountingServiceBase, IAccountingService
{
    private static readonly string[] AccountTypes = ["Asset", "Liability", "Equity", "Revenue", "Expense"];
    private static readonly string[] JournalTypes = ["General", "Sales", "Purchase", "Bank", "Cash"];
    private readonly IJournalEntryService journalEntries;

    public AccountingService(
        AppDbContext db,
        ICurrentTenant currentTenant,
        IJournalEntryService journalEntries)
        : base(db, currentTenant)
    {
        this.journalEntries = journalEntries;
    }
}
