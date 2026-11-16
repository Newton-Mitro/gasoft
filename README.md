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

### Reset database (keep existing migrations)

```sh
dotnet tool install --global dotnet-ef --version 10.0.14
# Drop existing database
dotnet ef database drop --force \
    --project src/Gasoft.Infrastructure \
    --startup-project src/Gasoft.Api

# Recreate database and apply all migrations
dotnet ef database update \
    --project src/Gasoft.Infrastructure \
    --startup-project src/Gasoft.Api
```

### Reset all migrations (create a fresh InitialCreate)

```sh
dotnet add src/Gasoft.Api package Microsoft.EntityFrameworkCore.Design --version 10.0.14
dotnet tool install --global dotnet-ef --version 10.0.14
# Remove existing migrations one by one
dotnet ef migrations remove \
    --project src/Gasoft.Infrastructure \
    --startup-project src/Gasoft.Api

# Create new initial migration
dotnet ef migrations add InitialCreate \
    --project src/Gasoft.Infrastructure \
    --startup-project src/Gasoft.Api

# Drop existing database
dotnet ef database drop --force \
    --project src/Gasoft.Infrastructure \
    --startup-project src/Gasoft.Api

# Create fresh database
dotnet ef database update \
    --project src/Gasoft.Infrastructure \
    --startup-project src/Gasoft.Api
```

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
