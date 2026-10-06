# Weapon Registration & Management System (WRMS)

A production-quality web application for authorized government security agencies and
law-enforcement organizations to digitize and centralize the registration, licensing,
ownership tracking, and administrative management of legally registered weapons.

This system covers **lawful registration, licensing, regulatory compliance, and
administrative management only**. It contains no features for weapon construction,
tactical use, or operational deployment.

## Tech Stack

- **.NET 8** / ASP.NET Core MVC
- **Entity Framework Core 8** + **SQL Server**
- **ASP.NET Core Identity** (role-based access: Admin, Officer, AuthorizedStaff)
- **Bootstrap 5**, Bootstrap Icons, Chart.js, DataTables.net (all vendored locally — no CDN dependency)
- **FluentValidation**, **AutoMapper**
- **PdfSharpCore / MigraDocCore** (PDF export), **ClosedXML** (Excel export)
- **Serilog** (console + rolling file logging)

## Solution Structure

```
WRMS.sln
src/
  WRMS.Domain          Entities, enums — no external dependencies
  WRMS.Application     DTOs, service interfaces, FluentValidation validators
  WRMS.Infrastructure  EF Core DbContext, Identity, service implementations,
                        PDF/Excel export, background jobs
  WRMS.Web             MVC controllers, Razor views, static assets
```

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB, Express, or full SQL Server) — connection string in `appsettings.json` targets `localhost` by default
- `dotnet-ef` 8.0.29 global tool for running migrations: `dotnet tool install --global dotnet-ef --version 8.0.29`

## First-Time Setup

1. Restore and build:
   ```
   dotnet restore
   dotnet build
   ```
2. Apply the database migrations (also happens automatically on app startup):
   ```
   dotnet ef database update --project src\WRMS.Infrastructure --startup-project src\WRMS.Web
   ```
3. Run the app:
   ```
   dotnet run --project src\WRMS.Web --launch-profile https
   ```
4. On first run, the app seeds:
   - The three roles: `Admin`, `Officer`, `AuthorizedStaff`
   - A default **Admin** account (`admin@wrms.local` unless overridden — see below)
   - Default weapon categories and system settings
   - In the **Development** environment only: a small set of illustrative sample
     owners/weapons/licenses, so the app isn't empty on first look. This never runs
     outside Development and never overwrites existing data.

### Default administrator credentials

If `SeedAdmin:Password` is not configured, a strong random password is generated on
first run and printed **once** to the console/log output with a clear warning banner.
Copy it from there, sign in, and change it immediately via the account menu.

To set a known password up front instead (recommended for any shared environment),
configure it before first run via user-secrets or environment variables — never commit
it to source control:

```
dotnet user-secrets set "SeedAdmin:Email" "admin@yourdomain.gov" --project src\WRMS.Web
dotnet user-secrets set "SeedAdmin:Password" "Your-Strong-P@ssw0rd" --project src\WRMS.Web
```

## Deploying to Railway

The repository includes a root-level `Dockerfile` that builds and runs the web app
on Railway. The application requires SQL Server; it does not use Railway's
PostgreSQL service. Railway has no managed SQL Server, so either run one as a
second service in the same project from the Docker image
`mcr.microsoft.com/mssql/server:2022-latest` (variables `ACCEPT_EULA=Y` and
`MSSQL_SA_PASSWORD`, a volume mounted at `/var/opt/mssql`, at least 2 GB of
memory) or use an externally hosted SQL Server such as Azure SQL.

1. In Railway, create a project and deploy the GitHub repository
   `ckhayir7-alt/myproject`. Leave the service root directory at the repository
   root so Railway can find `Dockerfile` and `WRMS.sln`. In the service's
   **Settings** under **Source**, enable auto-deploy for the `main` branch so
   pushed fixes are deployed automatically.
2. In the service's **Variables**, configure:
   - `ConnectionStrings__DefaultConnection`: the SQL Server connection string. For a
     SQL Server service in the same Railway project, use its private hostname:
     `Server=<service>.railway.internal,1433;Database=WRMS;User Id=sa;Password=<password>;TrustServerCertificate=True;MultipleActiveResultSets=true`
   - `SeedAdmin__Email`: the initial administrator's email address.
   - `SeedAdmin__Password`: a unique, strong password that meets the app's
     password policy (10+ characters, uppercase, lowercase, digit, and symbol).
   Railway supplies `PORT`; the container listens on that port automatically.
3. Ensure the SQL Server is reachable from Railway over the network and that the
   configured database user can run EF Core migrations. Migrations run
   automatically at application startup.
4. Generate a public domain for the Railway service and open it over HTTPS.

Do not commit database credentials or the administrator password to Git. Configure
them as Railway variables. Uploaded documents are stored in
`/app/App_Data/Uploads`; attach a Railway volume at that exact mount path if uploads
must survive deployments and restarts. Without a persistent volume, uploaded files
are ephemeral.

## Roles

| Role | Capabilities |
|---|---|
| **Admin** | Full access: user management, settings, all registry operations |
| **Officer** | Reviews/approves registrations, licenses, and ownership transfers; all data-entry actions |
| **AuthorizedStaff** | Data entry and record management; no approval authority |

## Modules

Authentication & RBAC · Dashboard · Weapon Registration · Owner Management ·
License Management (issue/renew/suspend/revoke) · Registration Approval Workflow ·
Weapon Transfer Management · Search & Verification (with printable verification
result) · Reports & Analytics (PDF/Excel export) · Notifications & Alerts (license
expiry, pending approvals) · Audit Logs · Settings (weapon categories, system
configuration, data export).

## Security Notes

- Role-based authorization on every controller (default-deny; `[AllowAnonymous]` only on the login/error pages)
- ASP.NET Core Identity password policy (10+ chars, mixed case, digit, symbol), account lockout after 5 failed attempts, and a fixed-window rate limiter on the login endpoint
- All business actions (create/update/approve/reject/suspend/revoke/transfer/export/login/logout) are written to the `AuditLogs` table, viewable under Audit Logs (Admin only)
- Soft delete on Owners/Weapons; hard deletes are not exposed in the UI
- Uploaded documents are stored outside `wwwroot`, extension/size-validated, and served through an authenticated download action
- Security response headers (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy) applied globally; HSTS enabled outside Development
- All third-party front-end libraries are vendored locally under `wwwroot/lib` — no runtime CDN dependency

## Running Tests / Verification

There is no automated test suite in this iteration. Each module was manually
verified end-to-end against a running instance during development (see git history /
commit-by-module structure). A recommended manual smoke test:

1. Sign in as the seeded Admin.
2. Register an owner, then a weapon for that owner (starts `PendingApproval`).
3. Approve the registration from **Approval Queue** — weapon becomes `Active`.
4. Issue a license from the weapon's detail page; renew and suspend it to confirm history tracking.
5. Request and approve an ownership transfer.
6. Run a search and generate a printable verification result.
7. Check the Dashboard totals and charts reflect the above.
8. Export a report to PDF and Excel from **Reports & Analytics**.
9. Confirm the actions above all appear in **Audit Logs**.
