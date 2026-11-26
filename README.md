# Gasoft

Gasoft is a multi-tenant accounting and business operations application. It provides a .NET 10 REST API backed by PostgreSQL and an Angular 22 web client. Each workspace can manage its companies, users, accounting, inventory, and operational workflows.

## Features

- **Workspace and access management:** workspace registration, sign-in, company selection, users, and role-based access.
- **Sales and receivables:** sales quotes, invoices, credit notes, refunds, customer payments, and a point-of-sale checkout.
- **Purchasing and payables:** purchase orders, purchase bills, and supplier payments.
- **Accounting:** chart of accounts, journals and entries, general ledger, fiscal years, accounting periods, and close workflows.
- **Inventory:** item catalog, warehouses and locations, stock receipts/issues, valuation, inventory counts, and stock balances.
- **Business operations:** expense reports and reimbursements, fixed assets and depreciation, and bank reconciliation.
- **Reporting:** financial statements, aging, cash flow, budgets, and consolidated reporting.
- **Administration:** company settings, tax configuration, notification preferences, email configuration, and audit history.

## Technology

- .NET 10 / ASP.NET Core
- Entity Framework Core 10 and PostgreSQL 16
- Angular 22, TypeScript, and npm

## Requirements

- Git
- Docker Engine and Docker Compose V2 for the containerized setup
- Or, for the native setup: .NET 10 SDK, Node.js 22.12 or later and npm, and PostgreSQL 16 or later

## Clone the repository

```sh
git clone https://github.com/Newton-Mitro/gasoft.git
cd gasoft
```

## Run with Docker Compose

Install Docker Engine (or Docker Desktop) and the Compose V2 plugin using the [official Docker instructions](https://docs.docker.com/get-docker/). Verify the installation:

```sh
docker --version
docker compose version
```

Create a local environment file and generate random values for the database password and JWT signing key:

```sh
cp .env.example .env
openssl rand -hex 32
```

Run `openssl rand -hex 32` a second time for a separate value, then put the generated values in `.env` as `POSTGRES_PASSWORD` and `JWT_SIGNING_KEY`. Keep `.env` private; it is excluded from Git.

Start the app from the repository root:

```sh
docker compose up --build -d
```

Compose starts PostgreSQL, applies the EF Core migration to its persistent database volume, and then starts the API and Angular development server. Open [http://localhost:4200](http://localhost:4200). The API health check is at [http://localhost:5035/health](http://localhost:5035/health).

Stop the containers with `docker compose down`. The PostgreSQL database and uploaded files are stored in persistent volumes. To permanently delete the database and uploads, use `docker compose down --volumes`; this is destructive.

This Compose configuration is intended for local development, not production deployment. It exposes the frontend and API on localhost and uses the Angular development server.

## Run locally

The native setup runs the API and frontend in separate terminals. The commands below assume a Linux development environment; use your PostgreSQL service's equivalent commands on other operating systems.

### 1. Create a fresh PostgreSQL database

Create a dedicated database role and an empty database. If you already have a suitable role or database, you can skip the corresponding command.

```sh
sudo -u postgres createuser --pwprompt gasoft
sudo -u postgres createdb --owner=gasoft gasoft
```

Choose and keep the password entered for `gasoft`; you will use it in the API connection string below.

### 2. Configure local API secrets

The API project has .NET User Secrets enabled. Store the database connection string and JWT configuration there, not in committed settings files:

```sh
dotnet user-secrets set "ConnectionStrings:Default" \
  "Host=localhost;Port=5432;Database=gasoft;Username=gasoft;Password=<your-local-database-password>" \
  --project src/Gasoft.Api
dotnet user-secrets set "Jwt:Issuer" "Gasoft.Local" --project src/Gasoft.Api
dotnet user-secrets set "Jwt:Audience" "Gasoft.Local" --project src/Gasoft.Api
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/Gasoft.Api
```

Replace `<your-local-database-password>` with the password you chose. The signing-key command requires `openssl`; alternatively, set a randomly generated key of at least 32 bytes. Never commit real credentials.

### 3. Initialize the database

Install the EF Core CLI tool if it is not already installed, then apply the consolidated baseline migration:

```sh
dotnet tool install --global dotnet-ef --version 10.0.14
dotnet ef database update \
  --context AppDbContext \
  --project src/Gasoft.Infrastructure \
  --startup-project src/Gasoft.Api
```

The single `InitialCreate` migration creates the schema and seeds reference data. It is intended for a **new, empty database**; it does not upgrade databases created with the previous migration chain. Do not run it against an existing database with data you need.

### 4. Start the API

In the first terminal, from the repository root:

```sh
dotnet run --project src/Gasoft.Api --launch-profile http
```

The API listens at `http://localhost:5035`. Check that it is running:

```sh
curl http://localhost:5035/health
```

### 5. Start the frontend

In a second terminal:

```sh
cd frontend
npm ci
npm start
```

Open [http://localhost:4200](http://localhost:4200). The Angular development server proxies `/api` requests to the local API at `http://localhost:5035`.

Create a workspace through the registration page, then sign in and choose a company to use the application.

## Tests and production build

Run backend tests from the repository root:

```sh
dotnet test Gasoft.slnx --configuration Release
```

The PostgreSQL outbox integration tests run when `GASOFT_TEST_POSTGRES` points to a disposable PostgreSQL test database. They create and drop isolated schemas and should not use the application database.

Run frontend tests and build from `frontend/`:

```sh
npm test -- --watch=false
npm run build
```

The production build checks the compressed initial asset budget. To run the Playwright smoke test, install Chromium once and run the test script:

```sh
npx playwright install chromium
npm run test:e2e
```
