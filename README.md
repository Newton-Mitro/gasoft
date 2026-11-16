# Gasoft

Multi-tenant accounting software starter: .NET 10 API, PostgreSQL, ASP.NET Core Identity/JWT, and Angular 22.

## Requirements

- .NET 10 SDK
- Node.js 22.12+ and npm
- PostgreSQL 16+

The Angular frontend uses Tailwind CSS 4 utilities for the application shell. Tailwind Preflight is intentionally excluded so existing feature-page styles and browser defaults remain unchanged.

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

### 3. Apply migrations to a fresh database

Install the EF Core command-line tool if needed, then create the database schema and seed migration-managed reference data (currencies, units of measure, and ISO country tax jurisdictions):

```sh
dotnet tool install --global dotnet-ef --version 10.0.14
dotnet ef database update --project src/Gasoft.Infrastructure --startup-project src/Gasoft.Api
```

If `dotnet-ef` is already installed, skip the install command. Open a new terminal if the global tool is not yet on your `PATH`. `database update` applies all pending migrations; it does not erase existing data. Do not use `EnsureCreated` for this application.

### 4. Start the API

```sh
dotnet run --project src/Gasoft.Api --launch-profile http
```

The API listens at `http://localhost:5035`. Check `http://localhost:5035/health` before continuing.

After updating API code, stop any already-running API process and run the command again so the process loads the rebuilt endpoints. The Angular application lives in `frontend/`. Start it in a second terminal:

```sh
cd frontend
npm ci
npm start
```

The development server serves `http://localhost:4200` and proxies `/api` requests to the API on port `5035`. Register a workspace at `/register` or sign in at `/login`. Registration signs you in automatically. On sign-in, enter your workspace address (the slug chosen during registration), email, and password. The API resolves the workspace and selects its first company; the application remembers both for later sessions. Workspace Owners can add more organizations from Company Settings; each new organization gets starter accounts, journals, and an open fiscal year with monthly periods, and becomes the selected company. Use the profile control at the bottom of the sidebar to update your display name, upload a profile image, or change your password. Company settings include organization contact, address, website, and tax-registration details, plus an organization logo upload. Uploaded JPEG, PNG, and WebP images are limited to 5 MB and stored under `src/Gasoft.Api/App_Data/uploads/`; persist and back up that directory when deploying the API. Images are served through authenticated endpoints. Forgot-password links are sent by SMTP and expire after 30 minutes; sales quote delivery uses the same TLS-enabled SMTP service. Configure `Email__FrontendBaseUrl`, `Email__SmtpHost`, `Email__SmtpPort`, `Email__SmtpUsername`, `Email__SmtpPassword`, `Email__FromAddress`, and optionally `Email__FromName` as environment variables. Without SMTP settings, password recovery and quote delivery return service unavailable. The workspace includes the chart of accounts, account groups, fiscal years, periods, journals and balanced journal-entry drafts, party directory and addresses, multi-line invoice and bill drafts/posting, payments and allocations, inventory setup, receipts/issues and counts, stock balances and movement history, team access, tax reference and company registrations, trial balance, income statement, balance sheet, receivables and payables aging, and company settings. If a newly added API route returns 404, verify that the running API's OpenAPI document includes the route at `http://localhost:5035/openapi/v1.json`.

Frontend code is organized by feature under `frontend/src/app`: API and authentication services live in `core/`, the application shell lives in `layout/shell/`, and login/registration screens live under `pages/auth/`. The overview is in `pages/dashboard/`; workspace definitions, forms, data access, and reusable components are separated under `pages/workspace/`. Shared page-level HTTP error messages live in `shared/utils/`. Components keep their templates and styles alongside their TypeScript files.

The Chart of Accounts is available at `/general-ledger/accounts`, with separate create (`/general-ledger/accounts/new`) and edit (`/general-ledger/accounts/{accountId}/edit`) pages. Its index uses `GET /api/v1/companies/{companyId}/accounts/page` for server-side pagination; `pageSize` is limited to 100 and optional `accountGroupId` and `search` filters are supported. Each page contains the matching root accounts and their descendants to preserve the account hierarchy. Account details and edits use `GET` and `PUT /api/v1/companies/{companyId}/accounts/{accountId}`.

Account Groups have their own index (`/general-ledger/account-groups`), create (`/general-ledger/account-groups/new`), and edit (`/general-ledger/account-groups/{groupId}/edit`) pages. The index uses `GET /api/v1/companies/{companyId}/account-groups/page` for server-side pagination with optional `search`; page sizes are limited to 100. Group details and edits use `GET` and `PUT /api/v1/companies/{companyId}/account-groups/{groupId}`. Normal balance is derived from account type, and account type cannot be changed once accounts use that group.

Tenant user management is available at `/users` and manages access across the tenant’s companies. Active Owners and Admins can list members, create new accounts with an initial password (share it privately), or add an existing Gasoft account by email without changing its password. Owners can also assign or remove the Owner role. User access and role changes take effect on subsequent API requests. The API uses `GET` and `POST /api/v1/tenant/users` to list and create memberships, `POST /api/v1/tenant/users/existing` to add an existing account, and `PUT /api/v1/tenant/users/{userId}` to change a role or deactivate/reactivate a member. At least one active Owner must remain.

Fiscal years and accounting periods have independent pages at `/general-ledger/fiscal-years` and `/general-ledger/periods`. Inventory items, categories, and warehouses are managed separately at `/inventory/items`, `/inventory/categories`, and `/inventory/warehouses`.

Supported tax jurisdictions are seeded as read-only ISO 3166-1 country reference data and are available to all workspace members. Owners configure company registrations at `/settings/tax-registrations` and company-specific tax codes and effective-dated rates at `/settings/tax-codes`. Jurisdiction presets and starter tax categories do not imply tax rules or rates. Rates must be verified against an authoritative source; overlapping effective periods for one tax code are rejected, and invoice/bill tax uses the rate valid on its document date.

Bank reconciliation is available at `/banking/reconciliation`. Owners and Admins can add a bank account linked to an active, postable asset account, then import a CSV with `date`, `description`, and signed `amount` columns plus an optional `reference` column. Dates must use `YYYY-MM-DD`; positive amounts are deposits and negative amounts are withdrawals. Imports are limited to 10 MB and 10,000 rows, and repeated statement rows are skipped. Statement amounts can be allocated across one or more posted bank-account ledger lines, and a ledger line can be allocated across statements. For fees or other differences, record the remaining amount to a selected offset account and an open-period date; this creates and posts a balanced bank-journal entry and matches its bank line automatically.

Consolidated trial balance is available at `/reports/consolidation`. Owners and Admins can create groups containing two or more organizations, select a reporting period, and enter functional-to-reporting-currency closing rates for balance-sheet balances and average rates for revenue/expense activity. Each run stores its rates and translated balances, including a foreign-currency translation adjustment (CTA). Draft runs support balanced intercompany-elimination entries and can be finalized after the consolidated trial balance balances. The translation and elimination workflow is a reporting snapshot and does not post consolidation adjustments to company ledgers.

The statement of cash flows is available at `/reports/cash-flow`, with direct and indirect presentations in company functional currency. It uses posted ledger activity on registered bank accounts. Classify counterpart accounts as operating, investing, or financing for direct receipts/payments and investing/financing flows; for the indirect method, also mark non-cash adjustments and operating working-capital assets/liabilities. Review unmapped movements and the indirect-method reconciliation difference. Classification depends on company accounting policy; transfers between registered bank accounts are netted out.

Purchase orders are available at `/purchase-orders`. Owners and Admins can create and place supplier orders, record partial receipts into the selected warehouse, and post inventory/journal movements for received stock. Order currency is supported using the entered exchange rate; inventory is valued in company functional currency. This workflow covers inventory-item purchases; service and expense-only orders are not included.

Sales quotes are available at `/sales-quotes`. Create and edit draft estimates, email a quote to the customer's saved email address, track sent/accepted/declined status, cancel them, and convert accepted quotes into draft sales invoices. Quote emails use the configured SMTP service; the quote is marked sent only after delivery succeeds, and the delivery recipient and time are recorded. Tax is estimated using rates effective on the quote date and recalculated using invoice-date rates during conversion. Quotes cannot be sent or converted after expiry.

Sales credit notes are available at `/sales-credit-notes`. Create quantity-based draft credits from posted invoice lines, then post a reversal to revenue, output-tax, and receivable accounts. Posting reduces the outstanding balance of the source invoice. The credit note cannot exceed its invoice’s remaining balance or the uncredited quantities.

Posted sales credit notes can be refunded in full or in part. Each refund posts a cash/receivables journal entry in an open accounting period, uses the original receivable account and exchange rate, and is limited to the remaining refundable amount.

### 5. Register a workspace and seed company defaults

On a fresh database, register the first tenant, owner, and company. Registration automatically seeds the new company with starter account groups and accounts, General/Sales/Purchase/Bank journals, and an open calendar fiscal year with 12 monthly periods. The response includes the `tenantId`, `companyId`, and owner `accessToken`.

```sh
curl -i http://localhost:5035/api/v1/tenants/register \
  -H 'Content-Type: application/json' \
  -d '{
    "tenantName": "Northstar Studio",
    "slug": "northstar-studio",
    "companyCode": "NS01",
    "legalName": "Northstar Studio Ltd",
    "functionalCurrencyCode": "USD",
    "ownerDisplayName": "Jordan Davis",
    "email": "jordan@example.com",
    "password": "Choose-A-Strong-Password-123!"
  }'
```

No separate company-seed command is required after registration. For a company that already exists, an authenticated tenant owner can idempotently add any missing defaults without changing existing records:

```sh
curl -X POST http://localhost:5035/api/v1/companies/1/setup/defaults \
  -H 'Authorization: Bearer <accessToken>'
```

Replace `1` with the `companyId` from registration and `<accessToken>` with its returned token. The seed endpoint returns counts of the records it added. The default fiscal year is based on the current UTC calendar year; review and adjust it for your accounting calendar. Tax rates and exchange rates are not fabricated by the seeder and must be provisioned from authoritative sources before use.

Budgets are created as named versions for a fiscal year and entered by account and accounting period. The budget-vs-actual view compares functional-currency budgets with posted revenue and expense activity; a positive variance is favorable. Budget creation, editing, and deletion are limited to company owners and admins.

Fixed assets are acquired through a posted ledger entry, depreciated straight-line by service days through an open period, and disposed at period end after depreciation is posted. Acquisition, depreciation, and disposal actions create balanced journal entries; the asset register tracks accumulated depreciation and net book value. These operations are limited to company owners and admins.
