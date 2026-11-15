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

Install the EF Core command-line tool if needed, then create the database schema and seed migration-managed reference data (currencies and units of measure):

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

The development server serves `http://localhost:4200` and proxies `/api` requests to the API on port `5035`. Register a workspace at `/register` or sign in at `/login`. Registration signs you in automatically. On sign-in, enter your workspace address (the slug chosen during registration), email, and password. The API resolves the workspace and selects its first company; the application remembers both for later sessions. Use the profile control at the bottom of the sidebar to update your display name, upload a profile image, or change your password. Company settings include organization contact, address, website, and tax-registration details, plus an organization logo upload. Uploaded JPEG, PNG, and WebP images are limited to 5 MB and stored under `src/Gasoft.Api/App_Data/uploads/`; persist and back up that directory when deploying the API. Images are served through authenticated endpoints. Forgot-password links are sent by SMTP and expire after 30 minutes. Configure `Email__FrontendBaseUrl`, `Email__SmtpHost`, `Email__SmtpPort`, `Email__SmtpUsername`, `Email__SmtpPassword`, `Email__FromAddress`, and optionally `Email__FromName` as environment variables; SMTP must support TLS. If mail settings are missing, the password recovery endpoint returns service unavailable rather than generating an unusable reset link. The workspace includes the chart of accounts, account groups, fiscal years, periods, journals and balanced journal-entry drafts, party directory and addresses, multi-line invoice and bill drafts/posting, payments and allocations, inventory setup, receipts/issues and counts, stock balances and movement history, team access, tax reference and company registrations, trial balance, income statement, balance sheet, and company settings. If a newly added API route returns 404, verify that the running API's OpenAPI document includes the route at `http://localhost:5035/openapi/v1.json`.

Frontend code is organized by feature under `frontend/src/app`: API and authentication services live in `core/`, the application shell lives in `layout/shell/`, and login/registration screens live under `pages/auth/`. The overview is in `pages/dashboard/`; workspace definitions, forms, data access, and reusable components are separated under `pages/workspace/`. Shared page-level HTTP error messages live in `shared/utils/`. Components keep their templates and styles alongside their TypeScript files.

The Chart of Accounts is available at `/general-ledger/accounts`, with separate create (`/general-ledger/accounts/new`) and edit (`/general-ledger/accounts/{accountId}/edit`) pages. Its index uses `GET /api/v1/companies/{companyId}/accounts/page` for server-side pagination; `pageSize` is limited to 100 and optional `accountGroupId` and `search` filters are supported. Each page contains the matching root accounts and their descendants to preserve the account hierarchy. Account details and edits use `GET` and `PUT /api/v1/companies/{companyId}/accounts/{accountId}`.

Account Groups have their own index (`/general-ledger/account-groups`), create (`/general-ledger/account-groups/new`), and edit (`/general-ledger/account-groups/{groupId}/edit`) pages. The index uses `GET /api/v1/companies/{companyId}/account-groups/page` for server-side pagination with optional `search`; page sizes are limited to 100. Group details and edits use `GET` and `PUT /api/v1/companies/{companyId}/account-groups/{groupId}`. Normal balance is derived from account type, and account type cannot be changed once accounts use that group.

Tenant user management is available at `/users` and manages access across the tenant’s companies. Active Owners and Admins can list members and create users with an initial password; share that password privately. Owners can also assign or remove the Owner role. User access and role changes take effect on subsequent API requests. The API uses `GET` and `POST /api/v1/tenant/users` to list and create memberships and `PUT /api/v1/tenant/users/{userId}` to change a role or deactivate/reactivate a member. At least one active Owner must remain.

Fiscal years and accounting periods have independent pages at `/general-ledger/fiscal-years` and `/general-ledger/periods`. Inventory items, categories, and warehouses are managed separately at `/inventory/items`, `/inventory/categories`, and `/inventory/warehouses`.

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

Replace `1` with the `companyId` from registration and `<accessToken>` with its returned token. The seed endpoint returns counts of the records it added. The default fiscal year is based on the current UTC calendar year; review and adjust it for your accounting calendar. Tax jurisdictions/rates and exchange rates are not fabricated by the seeder and must be provisioned from authoritative sources before use.
