# Gasoft

Multi-tenant accounting software starter: .NET 10 API, PostgreSQL, ASP.NET Core Identity/JWT, and Angular 22.

## Requirements

- .NET 10 SDK
- Node.js 22.12+ and npm
- PostgreSQL 16+

## Run locally

Run the API and frontend in separate terminals. The commands below assume you run them from the repository root.

### 1. Prepare PostgreSQL

Start PostgreSQL and create a database and user. For a fresh PostgreSQL installation on Ubuntu:

```sh
sudo -u postgres createuser --pwprompt gasoft
sudo -u postgres createdb --owner=gasoft gasoft
```

Choose a local database password when prompted. If you already have a PostgreSQL user and database, use those credentials in the connection string below instead.

### 2. Configure API secrets

The API requires a database connection string and a JWT signing key of at least 32 bytes. Keep these in .NET User Secrets, not in `appsettings.json`:

```sh
dotnet user-secrets init --project src/Gasoft.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=gasoft;Username=gasoft;Password=1056027" --project src/Gasoft.Api
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/Gasoft.Api
```

The key-generation command requires `openssl`. If you already initialized User Secrets for this project, skip `dotnet user-secrets init`.

In the connection-string command above, replace the password placeholder with the exact password entered for the PostgreSQL `gasoft` user. If you already have User Secrets configured, set or update the connection string explicitly:

### 3. Apply database migrations

Install the EF Core command-line tool if needed, then apply all pending migrations and seed reference data:

```sh
dotnet tool install --global dotnet-ef --version 10.0.14
dotnet ef database update \
    --project src/Gasoft.Infrastructure \
    --startup-project src/Gasoft.Api
```

If you are starting with a fresh database, this creates the schema and seeds ISO 3166-1 country tax jurisdictions. Existing databases are updated without deleting their data.

### Notifications and audit history

The notification inbox stores important workflow events (such as postings and period/year status changes) and account-security events. Inbox items are personal to each user and can be marked read or dismissed. Tenant Owners and Admins can review tenant-scoped create, update, delete, posting, status-change, and authentication records in **Administration → Audit history**. Changes to sensitive credential and bank-account fields are excluded from audit details.

### Point of sale

The point-of-sale checkout includes a **Walk-in customer** choice for sales where buyer details are not needed. It reuses a tenant-wide Walk-in Customer record, created automatically the first time the POS loads, so invoices and payments remain linked to a customer. Configure selling prices per company on inventory items; the POS displays and prefills the selected company's functional-currency price and blocks items without one.

### Accounting period controls

New organizations are created with starter account groups and accounts, plus General, Sales, Purchase, Bank, Cash receipts, and Cash payments journals. Creating a fiscal year automatically creates contiguous monthly accounting periods from its start date through its end date; the final period may be shorter. Add fiscal years before entering transactions.

Accounting periods can be closed or reopened by workspace Owners and Admins. A reason is required and each change is recorded in the period status history. The close checklist blocks periods with draft journal entries or unreconciled imported bank statement transactions; closed periods reject postings until an authorized user reopens them.

Fiscal years can be closed after they contain accounting periods, all periods are closed, and no draft journal entries remain. If posted revenue or expense balances remain, prepare a year-end closing journal from the fiscal-year close checklist, select a postable Equity account (3100 · Retained earnings is the default when available), review the balanced draft, and post it. The generated closing journal is dated on year-end in the final closed period and can be posted only through the authorized closing workflow. The fiscal year cannot be locked until that journal is posted. Close and reopen actions require a reason and are audited; reopening a fiscal year does not reopen its accounting periods. For the next fiscal year, open the year and prepare a draft opening-balance journal to carry forward the prior year’s posted balance-sheet balances. Review and post that opening draft before using the new period as a live operating year. For year-end reporting, the retained-earnings roll-forward can summarize the opening balance, yearly net income, and ending equity position for the selected fiscal year.

### 4. Start the API

```sh
dotnet run --project src/Gasoft.Api --launch-profile http
```

The API listens at `http://localhost:5035`. Check `http://localhost:5035/health` before continuing.

```sh
cd frontend
npm ci
npm start
```
